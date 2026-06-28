# SecShare CLI Usage Guide

SecShare provides a secure, client-side encrypted sharing tool that runs completely in your terminal. This guide covers all variations and options for uploading, downloading, and streaming secrets using the console utility.

---

## Commands Overview

The current CLI provides two main commands:
* **`upload`**: Encrypts and uploads files, directories, or plain text to the secure container, then produces a download link with a client-side decryption key.
* **`get`**: Downloads, decrypts, and extracts/displays payload content using a remote secure URL.

---

## Uploading Content

### 1. Upload a File
To upload a single file (such as a database backup, image, or document):
```bash
secshare upload ./backup.sql
```
This will compress the file, encrypt it locally using **AES-256-GCM**, upload the ciphertext, and print:
* A full URL containing the decryption key in the hash fragment.
* A separate URL and key (useful for sending the link in one channel and the key in another).

### 2. Upload an Entire Directory
To upload a folder and all its contents recursively:
```bash
secshare upload ./logs
```
* Note: The folder will be packed into a ZIP archive, encrypted as a single package, and automatically extracted into the destination directory upon download.

### 3. Share a Text Secret / Password / Token
To upload a text string directly without pointing to a file on your disk, use the `--text` option:
```bash
secshare upload "DATABASE_URL=postgres://user:pass@example/db" --text
```

---

## Customizing Upload Settings

### 1. Modify Expiration (TTL)
By default, uploads are kept for `24h`. You can customize this expiration using the `-e` or `--expires` option (e.g., `30m`, `2h`, `7d`):
```bash
secshare upload ./report.pdf --expires 2h
```

### 2. Limit the Number of Downloads
By default, the file can be downloaded once (`1`). You can change this limit using `-d` or `--downloads`:
```bash
secshare upload ./presentation.pptx --downloads 5
```
Once the download limit is reached, the encrypted payload is immediately purged from the storage.

---

## Piping and Standard Input Redirection (stdin)

The `secshare` client supports multiple Unix/Linux shell piping flows, which makes it extremely powerful for scripts, automated CI/CD jobs, and quick clipboard sharing.

When standard input is redirected to the command and no file path is supplied, it is automatically processed as text.

### 1. Unix Pipe (`|`)
Pipe the output of any command directly into `secshare`:
```bash
echo "my top-secret message" | secshare upload
```
Or upload system information directly:
```bash
cat /var/log/syslog | tail -n 100 | secshare upload
```

### 2. Here-String Redirection (`<<<`)
Pass inline literal text into the standard input stream:
```bash
secshare upload <<< "DATABASE_URL=mongodb://localhost:27017"
```

### 3. File Input Redirection (`<`)
Redirect a file's content directly into the standard input stream (which uploads it as a text secret):
```bash
secshare upload < ~/.ssh/id_rsa.pub
```

---

## Downloading and Decrypting Content

### 1. Normal Download with Key
To download and decrypt a shared cargo, pass the full URL containing the `#KEY` fragment. By default, it extracts files to the current directory (`.`):
```bash
secshare get "https://secshare.me/f/019f0e4f-35d2-7735-9cd5-dab101f0204d#SK5mhgwQm3ZIekxV-QG-SjFOawI-mMG69B40xUtvU20"
```

### 2. Download and Extract to a Custom Directory
Specify the target output directory as the second argument:
```bash
secshare get "https://secshare.me/f/019f0e4f-35d2-7735-9cd5-dab101f0204d#SK5mhgwQm3ZIekxV-QG-SjFOawI-mMG69B40xUtvU20" ./restored-data
```

### 3. Interactive Key Input
If you want to send the URL over a public channel and the key over a secure one, the recipient can download the link *without* specifying the key in the argument. The CLI will prompt them to enter the key safely and interactively:
```bash
secshare get "https://secshare.me/f/019f0e4f-35d2-7735-9cd5-dab101f0204d"
```
*Output prompt:*
```text
Source: https://secshare.me/f/019f0e4f-35d2-7735-9cd5-dab101f0204d
Destination: .

Enter decryption key: [keys are hidden while typing]
```
After typing/pasting the key, decryption and extraction proceed as usual.

