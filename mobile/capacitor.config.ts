import type { CapacitorConfig } from "@capacitor/cli"

const config: CapacitorConfig = {
  appId: "com.imedto.mobile",
  appName: "Imedto",
  webDir: "dist",
  // O app é cross-platform neutro (iOS + Android) — sem favorecer nenhum.
  server: {
    androidScheme: "https",
    iosScheme: "https",
    // Libera navegação/HTTP nativo para a API de produção (MVP).
    allowNavigation: ["app.imedto.com"],
  },
  plugins: {
    CapacitorSQLite: {
      // Banco local da própria aplicação (cache offline + rascunhos). Nada de PII sensível persistida sem necessidade.
      iosDatabaseLocation: "Library/CapacitorDatabase",
    },
    PushNotifications: {
      presentationOptions: ["badge", "sound", "alert"],
    },
    SplashScreen: {
      launchShowDuration: 0,
    },
  },
}

export default config
