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

## Build ve publish

```powershell
dotnet restore
dotnet build .\RaporAraclariLauncher.sln -c Release
dotnet publish .\src\RaporAraclari.Launcher\RaporAraclari.Launcher.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\artifacts\publish\win-x64
```

Framework-dependent publish kullanmak isterseniz `-r win-x64` degerini koruyup `--self-contained true` parametresini kaldirabilirsiniz. Inno Setup script'i yalnizca publish klasorunu bekler.

## Installer olusturma

1. Publish the launcher to `artifacts\publish\win-x64`.
2. Open `packaging\launcher.iss` in Inno Setup 6.
3. Compile the script, or run it from the command line:

```powershell
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" /DSourceDir="$(Resolve-Path .\artifacts\publish\win-x64)" /DAppVersion="1.0.0" .\packaging\launcher.iss
```

Installer ciktilari `artifacts\installer\` altina yazilir.

## Notlar

- The launcher executable name is `RaporAraclari.Launcher.exe`.
- The published launcher build is placed in `artifacts\publish\win-x64`.
- `sorumluluk-hesaplayici` setup EXE yayinlamaya basladiginda manifest degistirilmeden setup akisi otomatik tercih edilecektir; launcher once setup asset'ini, bulamazsa portable EXE'yi arar.
