const fs = require("fs");
const zlib = require("zlib");

const [manifestPath, indexPath, sourcePath] = process.argv.slice(2);

if (!manifestPath || !indexPath) {
    console.error("Usage: node fingerprint-published-index.js <manifestPath> <indexPath> [sourcePath]");
    process.exit(1);
}

const manifest = JSON.parse(fs.readFileSync(manifestPath, "utf8"));
const html = fs.readFileSync(resolveHtmlSourcePath(sourcePath, indexPath), "utf8");
const fingerprints = new Map();

for (const endpoint of manifest.Endpoints || []) {
    const properties = new Map((endpoint.EndpointProperties || []).map(property => [property.Name, property.Value]));
    const label = properties.get("label");
    const fingerprint = properties.get("fingerprint");

    if (!label || !fingerprint || label.endsWith(".br") || label.endsWith(".gz")) {
        continue;
    }

    fingerprints.set(label, fingerprint);
}

const assets = [
    { url: "css/app.min.css", label: "css/app.min.css" },
    { url: "js/secshare-interop.js", label: "js/secshare-interop.js" },
    { url: "images/favicons/favicon.ico", label: "images/favicons/favicon.ico" },
    { url: "images/favicons/favicon.svg", label: "images/favicons/favicon.svg" },
    { url: "images/app-icons/app-icon.svg", label: "images/app-icons/app-icon.svg" },
    { url: "images/app-icons/apple-touch-icon.png", label: "images/app-icons/apple-touch-icon.png" },
    { url: "site.webmanifest", label: "site.webmanifest" },
];

let updatedHtml = html;

for (const asset of assets) {
    const fingerprint = fingerprints.get(asset.label);

    if (!fingerprint) {
        console.warn(`Fingerprint was not found for ${asset.label}`);
        continue;
    }

    updatedHtml = updatedHtml.split(asset.url).join(`${asset.url}?v=${fingerprint}`);
}

fs.writeFileSync(indexPath, updatedHtml);
fs.writeFileSync(`${indexPath}.gz`, zlib.gzipSync(updatedHtml));
fs.writeFileSync(
    `${indexPath}.br`,
    zlib.brotliCompressSync(updatedHtml, {
        params: {
            [zlib.constants.BROTLI_PARAM_QUALITY]: zlib.constants.BROTLI_MAX_QUALITY,
        },
    })
);

function resolveHtmlSourcePath(sourcePath, fallbackPath) {
    if (!sourcePath || !fs.existsSync(sourcePath)) {
        return fallbackPath;
    }

    const stats = fs.statSync(sourcePath);

    if (!stats.isDirectory()) {
        return sourcePath;
    }

    const htmlFiles = fs
        .readdirSync(sourcePath)
        .filter(fileName => fileName.endsWith(".html"))
        .sort();

    if (htmlFiles.length === 0) {
        return fallbackPath;
    }

    return `${sourcePath}/${htmlFiles[0]}`;
}
