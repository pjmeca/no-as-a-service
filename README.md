<p align="center">
  <img src="assets/banner.jpg" alt="No-as-a-Service (Multilingual .NET Edition) Banner"/>
</p>

[![GitHub Repo stars](https://img.shields.io/github/stars/pjmeca/no-as-a-service?style=flat&logo=github&label=Star%20this%20repo!)](https://github.com/pjmeca/no-as-a-service)
[![Docker Image Version (tag)](https://img.shields.io/docker/v/pjmeca/no-as-a-service/latest?logo=docker)](https://hub.docker.com/r/pjmeca/no-as-a-service)

Ever needed a graceful way to say “no”?  
This tiny API returns random, generic, creative, and sometimes hilarious rejection reasons — now fully rewritten in **.NET 10** with **Native AOT** support for minimal runtime overhead.

Built for humans, excuses, humor, and fast deployments.

---

## 🚀 API Usage

**Base URL** (if self-hosted via Docker):
```
https://naas.pjmeca.com
```

**Method:** `GET`  
**Rate Limit:** 120 requests per minute per IP (configurable in the code)

### 🔄 Example Request
```http
GET /
```

### ✅ Example Response (plain text)

```txt
Not even if there were free donuts.
```

Use it in apps, bots, landing pages, Slack integrations, rejection letters, or wherever you need a polite (or witty) no.

### 🌍 Multi-language support

This version supports **multiple languages**, selectable via the `lang` query parameter:

```http
GET /?lang=en
GET /?lang=es
GET /?lang={code}
````

Currently available languages:

* 🇬🇧 English (`en`) – default
* 🇪🇸 Spanish (`es`)
* 🇩🇪 German (`de`)
* 🇷🇺 Russian (`ru`)

If no `lang` parameter is provided, *English* is used by default.

Each language has its own text file inside the `reasons/` directory, making it easy to add new languages without changing any code.

### 🙏 Acknowledgements

Special thanks to [akicool](https://github.com/akicool) for contributing the German (`de`) and Russian (`ru`) translations in [PR #51](https://github.com/hotheadhacker/no-as-a-service/pull/51/commits/148ddca1c20fc30dd6fb3787baf9050b2a34286b) of the original repository.

---

## 🐳 Run with Docker (no setup required)

If you just want to run the API without building anything locally, a prebuilt image is available on Docker Hub.

```bash
docker run -p 5000:5000 pjmeca/no-as-a-service
```

The API will be available at:

```
http://localhost:5000
```

No Node.js, no .NET SDK, no build steps required.

---

## 🛠️ Self-Hosting

This .NET version is easy to run locally or in Docker.

### 1. Clone the repository

```bash
git clone https://github.com/pjmeca/no-as-a-service.git
cd no-as-a-service
```

### 2. Development (Windows / Rider / VSCode)

```bash
dotnet run
```

* Runs without Native AOT, fully debuggable.
* Access the API at `http://localhost:5000`.

### 3. Production / Docker (Native AOT)

```bash
docker build -t pjmeca/no-as-a-service .
docker run -p 5000:5000 pjmeca/no-as-a-service
```

* Produces a single, optimized native binary.
* Lightweight image ready for fast deployments.
* API available at `http://localhost:5000`.

---

## 📁 Project Structure

```
NoAsAService/
├── Program.cs          # Minimal API in .NET 10
├── reasons/            # One text file per language with rejection lines
├── NoAsAService.csproj
├── Dockerfile
└── README.md
```

---

## 🐳 Dockerfile Highlights

* Multi-stage build: SDK for compilation, minimal runtime for execution.
* Native AOT + single-file binary for production.
* Port 5000 exposed and configurable via `ASPNETCORE_URLS`.

---

## ⚓ Development Notes

* In development, reflection-based features are enabled.
* In production, reflection-based features are disabled because of Native AOT. "Reasons" are loaded directly from text files.
* Rate limiting is optional but implemented per IP (or `CF-Connecting-IP` when behind Cloudflare).

---

## 👤 Author

Forked and maintained by [pjmeca](https://github.com/pjmeca), adapted from [hotheadhacker](https://github.com/hotheadhacker).

---

## 📄 License

MIT — do whatever, just don’t say yes when you should say no.
