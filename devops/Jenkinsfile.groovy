@Library('common')
import com.shared.jenkins.docker.DockerHelper
import com.shared.jenkins.docker.DockerContainer

def effectiveEnvironment = params.ENVIRONMENT ?: 'Development'
def environmentKey = effectiveEnvironment.toLowerCase()
def containerSharedDir = "/mnt/local_share/docker_images/secshare"
def imageName = "latest"
def imageCommonTmpName = "${containerSharedDir}/${environmentKey}_common_latest"
def imageApiTmpName = "${containerSharedDir}/${environmentKey}_api_latest"
def currentBranchName = ''
def shouldRunDeployment = true

def dockerHelper = new DockerHelper(this)
public Map<String, String> envVariables = new HashMap<String, String>()

def mainContainer = new DockerContainer(
    name: "secshare-main-${environmentKey}",
    dockerFile: 'devops/common/Dockerfile',
);

def migrationContainer = new DockerContainer(
    name: "secshare-main-${environmentKey}",
    dockerFile: 'devops/common/Dockerfile',
    isRunAlways: false,
    isRunInBackground: false,
);

def apiContainer = new DockerContainer(
    name: "secshare-api-${environmentKey}",
    dockerFile: 'devops/api/Dockerfile',
);

def repositoryUrl = scm.userRemoteConfigs[0].url;
def gitCredentials="gitea-jenkins-ssh-key"

properties([
    pipelineTriggers([
        githubPush()
    ]),
    parameters([
        // https://plugins.jenkins.io/git-parameter/
        gitParameter (name: 'GIT_TAG', type: 'PT_TAG', sortMode: 'DESCENDING_SMART', selectedValue: 'NONE', defaultValue: 'main'),
        string (name: 'NEW_VERSION', defaultValue: '', description: 'Provide version to create GIT tag'),
        choice(name: 'ENVIRONMENT', choices: ['Development', 'Production'], description: 'Select environment to deploy'),
    ]),
    disableConcurrentBuilds()
])

node('build-node') {

    stage('Show deployment parameters') {
        echo "Repository: ${repositoryUrl}"
        echo "Requested environment: ${params.ENVIRONMENT}"
        echo "Tag: ${params.GIT_TAG}"
    }

    if (!params.GIT_TAG?.trim())
    {
        stage('Switch to GIT tag') {
            git branch: "${params.BRANCH}", url: repositoryUrl
        }    
    }

    stage('Checkout') {
        cleanWs()
        sh """
            git config --global http.postBuffer 2048M
            git config --global http.maxRequestBuffer 1024M
            git config --global core.compression 0
        """
        checkout scm
    }

    stage('Resolve trigger context') {
        currentBranchName = resolveBranchName()
        def isAutoBuildForPush = isAutoTriggeredPushBuild()

        if (isAutoBuildForPush && currentBranchName != 'main') {
            shouldRunDeployment = false
            currentBuild.result = 'NOT_BUILT'
            echo "Skipping auto deployment for branch '${currentBranchName}'. Only 'main' is deployed automatically."
        }

        if (isAutoBuildForPush && currentBranchName == 'main') {
            effectiveEnvironment = 'Development'
        }

        environmentKey = effectiveEnvironment.toLowerCase()
        imageCommonTmpName = "${containerSharedDir}/${environmentKey}_common_latest"
        imageApiTmpName = "${containerSharedDir}/${environmentKey}_api_latest"
        mainContainer.name = "secshare-main-${environmentKey}"
        migrationContainer.name = "secshare-main-${environmentKey}"
        apiContainer.name = "secshare-api-${environmentKey}"

        echo "Branch: ${currentBranchName}"
        echo "Auto-triggered push build: ${isAutoBuildForPush}"
        echo "Effective environment: ${effectiveEnvironment}"
    }

    if (!shouldRunDeployment) {
        stage('Skip deployment') {
            echo "Deployment pipeline skipped."
        }
        return
    }

    stage('Set environment vars') {
        // Serilog / Logging
        envVariables.put('Serilog__MinimumLevel__Default', 'Debug')
        
        mainContainer.buildVariables.put('ENVIRONMENT', effectiveEnvironment)
        apiContainer.buildVariables.put('ENVIRONMENT', effectiveEnvironment)
        envVariables.put('ASPNETCORE_ENVIRONMENT', effectiveEnvironment)

        // GrayLog
        envVariables.put('App__Logging__GrayLog__Host', '192.168.88.30')
        envVariables.put('App__Logging__GrayLog__Port', '12201')

        def dbName = ''
        def dbPort = ''
        def dbHost = ''
        if (effectiveEnvironment == 'Production')
        {
            envVariables.put('App__FrontendUrl', 'https://secshare.com')
            dbName = 'secshare'
            dbPort = '5432'
            dbHost = '192.168.88.41'
            
            envVariables.put('Garage__BucketName', "secshare-${environmentKey}")
        }
        else if (effectiveEnvironment == 'Development')
        {
            envVariables.put('App__FrontendUrl', 'https://dev.secshare.com')
            dbName = 'secshare_dev'
            dbPort = '5432'
            dbHost = '192.168.88.42'
            
            envVariables.put('Garage__BucketName', "secshare-${environmentKey}")
        }
        envVariables.put('Garage__Url', "http://192.168.88.44:3900")

        // DB Credentials
        withCredentials([
                usernamePassword(credentialsId: "secshare_${environmentKey}_db_credentials", usernameVariable: 'USER_NAME', passwordVariable: 'PASSWORD')
        ]) {
            envVariables.put(
                'ConnectionStrings__DefaultConnection',
                "User ID=${USER_NAME};Password=${PASSWORD};Host=${dbHost};Port=${dbPort};Database=${dbName};Pooling=true;"
            )
        }
        
        // Garage
        withCredentials([
            usernamePassword(credentialsId: "secshare_${environmentKey}_garage_credentials", usernameVariable: 'USER_NAME', passwordVariable: 'PASSWORD')
        ]) {
            envVariables.put('Garage__AccessKey', USER_NAME)
            envVariables.put('Garage__SecretKey', PASSWORD)
        }
    }

    stage('Build main image') {
        dockerHelper.buildAndSave(mainContainer, imageCommonTmpName)
    }

    stage('Build api image') {
        dockerHelper.buildAndSave(apiContainer, imageApiTmpName)
    }

    if (params.NEW_VERSION) {
        stage('Create GIT tag') {
            def (VER_MAJOR, VER_MINOR, VER_PATCH, VER_BUILD) = params.NEW_VERSION.tokenize('.').collect { it.toInteger() }
            env.VERSION_INCREMENT = VER_MAJOR + "." + VER_MINOR + "." + VER_PATCH + "." + VER_BUILD

            withCredentials([sshUserPrivateKey(credentialsId: gitCredentials, keyFileVariable: 'key')]) {
                sh '''
                    git config core.sshCommand 'ssh -i ${key}'
                    git config user.email "lampego@gmail.com"
                    git config user.name "lampego"
                    git tag "${VERSION_INCREMENT}"
                    git push --tags
                '''
            }
        }
    }

    stage("Clean workspace") {
        cleanWs()
    }
    
    stage('CleanUp Docker') {
        sh 'docker system prune -f'
    }

    stage('Purge Cloudflare cache') {
        if (effectiveEnvironment == 'Production')
        {
            withCredentials([
                usernamePassword(credentialsId: "secshare_cloudflaire_api_token", usernameVariable: 'USER_NAME', passwordVariable: 'PASSWORD')
            ]) {
                sh '''
                    set -e

                    RESPONSE=$(curl -sS -X POST "https://api.cloudflare.com/client/v4/zones/${USER_NAME}/purge_cache" \
                    -H "Authorization: Bearer ${PASSWORD}" \
                    -H "Content-Type: application/json" \
                    --data '{"purge_everything":true}')

                    echo "$RESPONSE" | grep -q '"success":true'
                '''
            }
        }
    }
}

if (shouldRunDeployment) {
node('web-node') {

    stage('Load container') {
        dockerHelper.loadFromFile(imageCommonTmpName)
        dockerHelper.loadFromFile(imageApiTmpName)
    }

    stage('Stop containers') {
        apiContainer.tagName = "secshare-api-${environmentKey}";
        dockerHelper.stopContainer(apiContainer)

        mainContainer.tagName = "secshare-worker-${environmentKey}";
        dockerHelper.stopContainer(mainContainer)
    }

    stage('Run migrations') {
        dockerHelper.stopContainer(migrationContainer)

        migrationContainer.envVariables = envVariables.clone()
        migrationContainer.envVariables.put('PROJECT_DIR', 'SecShare.Migrations')
        dockerHelper.runContainer(migrationContainer)
    }

    stage('Run api app') {
        apiContainer.tagName = "secshare-api-${environmentKey}";
        if (effectiveEnvironment == 'Production')
        {
            apiContainer.port = '8217:80';
        }
        else if (effectiveEnvironment == 'Development')
        {
            apiContainer.port = '8218:80';
        }

        apiContainer.envVariables = envVariables.clone()
        apiContainer.envVariables.put('PROJECT_DIR', 'SecShare.Api')
        dockerHelper.runContainer(apiContainer)
    }

    stage('Run worker') {
        mainContainer.tagName = "secshare-worker-${environmentKey}";
        mainContainer.port = '';

        mainContainer.envVariables = envVariables.clone()
        mainContainer.envVariables.put('PROJECT_DIR', 'SecShare.WorkerServices')
        dockerHelper.runContainer(mainContainer)
    }
}
}

def resolveBranchName() {
    def rawBranchName = env.BRANCH_NAME ?: env.GIT_BRANCH
    if (!rawBranchName?.trim()) {
        rawBranchName = sh(script: 'git rev-parse --abbrev-ref HEAD', returnStdout: true).trim()
    }

    return rawBranchName
        .replaceFirst(/^origin\//, '')
        .replaceFirst(/^refs\/heads\//, '')
}

def isAutoTriggeredPushBuild() {
    if (currentBuild.rawBuild.getCause(hudson.model.Cause$UserIdCause) != null) {
        return false
    }

    return currentBuild.rawBuild.getCauses().any { cause ->
        def causeName = cause.class.simpleName
        return causeName != null && (causeName.contains('GitHubPush')
            || causeName.contains('Gitea')
            || causeName.contains('SCMTrigger'))
    }
}


