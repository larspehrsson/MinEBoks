# MinEBoks
Program til automatisk at hente dokumenter fra eBoks og gemme dem i en mappe og evt. også videresende dem til en valgfri mail adresse.

Programmet skal konfigureres ved første start.

Login til EBoks finder du ved at logge ind på eboks og gå til [Menu -> Mobiladgang](https://www.e-boks.dk/privat/#/settings/mobileaccess)

Ønsker man at downloaded alle dokumenter til en mappe (fx sin dropbox) skal man udfylde Cpr-nr, Adgangskode og Aktiveringskode. Disse tre oplysninger finder man på ovenstående side. Hvis man også ønsker at få nye dokumenter tilsendt som post skal man fjerne krydset i "Download kun" og udfylde Mailserver opsætningen. 

Programmet holder styr på hvilke dokumenter der er hentet, så man kan flytte dokumenterne efter at de er hentet uden at det bliver hentet igen. Listen over hentede dokumenter kan nulstilles i opsætningen.

Så længe programmet kører vil det polle efter nye dokumenter hver 4. time.

Windows installationsprogram til denne version [setup.exe](https://dl.dropboxusercontent.com/u/10635853/EBoks/setup.exe)

## Related projects

- [MinBoks](https://github.com/larspehrsson/MinBoks) by [Lars Pehrsson](https://github.com/larspehrsson) windows service version af denne.
- [e-boks-mailer](https://github.com/christianpanton/eboks-mailer) by [Christian Panton](https://twitter.com/christianpanton) is written in Python and works by scraping the mobile website. Can forward messages by email.
- [Net-Eboks](https://github.com/dk/Net-Eboks) by [Dmitry Karasik](https://twitter.com/dmitrykarasik) is written in perl and also uses the mobile app API. Can also expose documents through POP3. Dmitry even hosts an open server and promises that it will not store your credentials or your documents on his server.
- [Postboks](https://github.com/olegam/Postboks) af [Ole Gam](https://twitter.com/olegam) MacOS project syncs your e-Boks documents to a folder on your mac in the background. You never have to log in to the e-Boks website using NemID again.
