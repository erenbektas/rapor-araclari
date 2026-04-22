# Rapor Araclari Launcher

`Belge Numaralandirici` ve `Sorumluluk Hesaplayici` icin tek Windows giris noktasi. Launcher iki uygulamayi GitHub release varliklarindan kurar, acar, surum durumunu saklar ve yeni release geldiginde guncelleme penceresi gosterir.

## Mevcut entegrasyon davranisi

- Hedef repolar:
  - [belge-numaralandirici](https://github.com/erenbektas/belge-numaralandirici)
  - [sorumluluk-hesaplayici](https://github.com/erenbektas/sorumluluk-hesaplayici)
- `belge-numaralandirici` setup EXE yayinladigi icin launcher bu uygulamayi sessiz kurulum akisi ile yonetir.
- `sorumluluk-hesaplayici` guncel release'te yalnizca tek EXE yayinladigi icin launcher bu uygulamada portable fallback kullanir ve dosyayi kendi uygulama veri klasorune yerlestirir.
- Launcher durumu `%LocalAppData%\RaporAraclariLauncher\launcher-state.json` dosyasinda tutar.
- Launcher penceresi acik kalir; araclar ayri surec olarak baslatilir.
- Windows Schannel/TLS katmani GitHub baglantisinda hata verirse launcher `gh` kurulu oldugu durumlarda GitHub CLI fallback'i kullanir.
- `v1.0.2` installer'i, hedef makinede runtime yoksa resmi `https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe` paketini indirip sessizce kurar.

## Build ve publish

```powershell
dotnet restore
dotnet build .\RaporAraclariLauncher.sln -c Release
dotnet publish .\src\RaporAraclari.Launcher\RaporAraclari.Launcher.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o .\artifacts\publish\win-x64
```

Varsayilan dagitim modeli self-contained olmalidir. Bu makinede runtime pack'ler lokal olarak bulunmadigi icin release installer'i ayrica .NET 8 Windows Desktop Runtime bootstrapper'i de icerir. Inno Setup script'i yalnizca publish klasorunu bekler.

## Installer olusturma

1. Publish the launcher to `artifacts\publish\win-x64`.
2. Open `packaging\launcher.iss` in Inno Setup 6.
3. Compile the script, or run it from the command line:

```powershell
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" /DSourceDir="$(Resolve-Path .\artifacts\publish\win-x64)" /DMyAppVersion="1.0.2" .\packaging\launcher.iss
```

Installer ciktilari `artifacts\installer\` altina yazilir.

## Notlar

- The launcher executable name is `RaporAraclari.Launcher.exe`.
- The published launcher build is placed in `artifacts\publish\win-x64`.
- `v1.0.2` ve sonrasi icin GitHub release setup paketleri self-contained publish uzerinden uretilmelidir.
- `sorumluluk-hesaplayici` setup EXE yayinlamaya basladiginda manifest degistirilmeden setup akisi otomatik tercih edilecektir; launcher once setup asset'ini, bulamazsa portable EXE'yi arar.
- Bu makinede GitHub HTTPS baglantisi Schannel tarafinda bozuk oldugu icin git icin `openssl` backend kullanilir. Launcher tarafinda ise ek olarak `gh` fallback'i vardir.
