# Secure Chat Application

A secure Progressive Web App (PWA) chat application that doesn't store messages or user data on the server. All communication is encrypted client-side and routed temporarily through the server.

## Presentation Materials

Get to work with presentation resources quickly:
- [Project Overview Slides](https://drive.google.com/file/d/1rdkmB60H1igTWW-jmE6zryCtmKpLMJh8/view?usp=sharing) (Slides/PDF)

## Key Features

- 🚀 **Progressive Web App** - Installable on any device with offline capabilities
- 🗑️ **No Server Storage** - Messages aren't persisted after delivery
- 🔑 **JWT Authentication** - Secure session management
- 📱 **Cross-Platform** - Works on desktop and mobile devices
- 💾 **Local Storage** - Encryption keys and settings stored only on client devices

## Installation & Setup

### Prerequisites
- Docker (for containerized deployment)

### 1. Clone the Repository
```bash
git clone https://github.com/orldev/Anonym.git
cd secure-pwa-chat
```

### 2. Configure JWT (Required)
Edit `server/appsettings.json`:
```json
"Jwt": {
  "Issuer": "your_issuer",
  "Audience": "your_audience",
  "SecretKey": "generate_a_strong_key_here",
  "ValidateAudience": true,
  "ValidateIssuer": true,
  "ValidateLifetime": true,
  "ValidateIssuerSigningKey": true,
  "TokenLifetime": 60
}
```

> **Security Note**: Generate a strong secret key using a tool like OpenSSL or a password manager.

### 3. Configure Encryption (Required)
Modify `Client/Core/Extensions/ServiceCollectionExtensions.cs`:
```csharp
public static void AddLocalStorage(this IServiceCollection services)
{
    services.AddLocalStorage(o => 
    {
        o.Passphrase = "your_strong_passphrase";
        o.IV = "unique_16_char_iv";      // 16 characters exactly
        o.Salt = "unique_16_char_salt";  // 16 characters exactly
        o.Iterations = 1000;
        o.DesiredKeyLength = 16;          // 16 = AES-128, 32 = AES-256
        o.HashMethod = HashAlgo.SHA384;
    });
}
```

### 4. Run with Docker
```bash
docker-compose up --build
```

After successful build:
- Server: `http://localhost:9999`

## PWA Installation Guide

### Desktop (Chrome/Edge/Firefox)
1. Open `http://localhost:9999` in your browser
2. Look for the install icon in the address bar (Chrome/Edge) or:
    - **Chrome/Edge**: Click ⋮ → "Install Secure Chat"
    - **Firefox**: Click ⋮ → "Install"
3. Follow the prompts to complete installation

### Mobile Devices
1. Open the app in your mobile browser
2. For:
    - **Android/Chrome**: Tap ⋮ → "Add to Home screen"
    - **iOS/Safari**: Tap Share → "Add to Home Screen"
3. Launch like a native app

## Security Best Practices

1. **Always** change default credentials before deployment
2. Use strong, randomly generated values for:
    - JWT SecretKey
    - Encryption Passphrase
    - IV and Salt values
3. Consider increasing:
    - KeyLength to 32 (AES-256)
    - Iterations to 10,000+ for better security

## License
[GNU General Public License v3 (GPLv3)](LICENSE)