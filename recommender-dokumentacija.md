# CineVision — dokumentacija sistema preporuke (recommender)

## 1. Pregled

CineVision koristi **hibridni sistem preporuke filmova** koji kombinuje:

1. **Popularity-based** — koliko je film popularan u cijelom katalogu  
2. **Content-based** — sličnost sadržaja (žanr + ključne riječi) profilu korisnika  
3. **Search-history-based** — afinitet prema stvarnoj historiji pretraga korisnika  

Preporuke su **personalizovane** (zahtijevaju autentifikovanog korisnika) i **objašnjive**: uz svaku preporuku API vraća kratki tekstualni razlog (`Reason`) koji se prikazuje u mobilnoj aplikaciji.

Implementacija: `CineVision.Services/RecommendationService.cs`  
API: `GET /Movies/Recommendations` (`RecommendationsController`)  
Klijent: Flutter mobile (`cinevision_mobile`)

---

## 2. Ulazni podaci (signal sources)

Svi podaci koji ulaze u recommender **stvarno se upisuju u bazu** tokom korištenja aplikacije.

| Signal | Izvor u bazi | Kako nastaje |
|--------|--------------|--------------|
| Broj rezervacija po filmu | `ReservationSeats` + `Reservations` + `Projections` | Korisnik rezerviše sjedišta (status ≠ Cancelled) |
| Prosječna ocjena | `Reviews.Rating` | Korisnik ostavlja recenziju |
| Broj pregleda | `Movies.ViewCount` | Inkrementira se pri pregledu detalja filma |
| Profil ukusa (content) | rezervacije korisnika + recenzije s ocjenom ≥ 4 | Bookings i high ratings |
| Historija pretrage | `SearchHistories` | Mobilna app šalje `POST /Movies/SearchHistory` pri pretrazi po naslovu/žanru |

Za search profil uzima se do **40 najnovijih** zapisa iz `SearchHistories` za trenutnog korisnika.

---

## 3. Algoritam

### 3.1. Kandidati

Svi **aktivni** filmovi (`Movies.IsActive = true`) ulaze u rangiranje.

### 3.2. Popularity score

Za svaki film računaju se tri sirova signala:

- broj rezerviranih sjedišta (katalog-wide, bez cancelled rezervacija)
- `ViewCount`
- prosječni rating iz recenzija

Svaki signal se **min-max normalizuje** na `[0, 1]` među kandidatima, zatim:

```text
PopularityScore = (nReservations + nViews + nRating) / 3
```

### 3.3. Content score (content-based)

Korisnički profil se gradi iz filmova koje je:

- rezervirao (status ≠ Cancelled), ili  
- ocijenio sa **Rating ≥ 4**

Iz tih filmova izvlače se:

- **žanrovi** — frekvencija `GenreId` (normalizovana na max frekvenciju)
- **ključne riječi** — tokeni iz `Description` (dužina ≥ 3, bez stop-words)

Za kandidata:

```text
ContentRaw = 0.6 * GenreComponent + 0.4 * KeywordComponent
```

- `GenreComponent` — udeo žanra kandidata u profilu  
- `KeywordComponent` — **Jaccard** sličnost tokena opisa kandidata i profilnih tokena  

Zatim se `ContentRaw` min-max normalizuje u `ContentScore ∈ [0, 1]`.

Ako korisnik nema rezervacija ni “liked” recenzija, content komponenta ostaje 0.

### 3.4. Search score (search-history-based)

Iz nedavnih pretraga grade se:

- težine žanrova (`GenreId` ili sintetički query oblika `genre:{id}`)
- tokeni iz teksta pretrage
- fraze za podudaranje naslova

Za kandidata:

```text
SearchRaw =
    0.5 * SearchGenreComponent +
    0.3 * SearchKeywordComponent +
    0.2 * SearchTitleComponent
```

- žanr: frekvencija žanra u pretragama (normalizovano)  
- keyword: Jaccard naslova (fallback: opis) sa tokenima pretrage  
- title: 1.0 ako naslov sadrži neku od fraza pretrage, inače 0  

Zatim min-max normalizacija u `SearchScore ∈ [0, 1]`.

### 3.5. Finalni hibridni skor

Težine se čitaju iz konfiguracije (`.env` / `appsettings`):

| Ključ | Default |
|-------|---------|
| `Recommendations__PopularityWeight` | 0.4 |
| `Recommendations__ContentWeight` | 0.4 |
| `Recommendations__SearchWeight` | 0.2 |

**Cold start** — korisnik nema ni content profil (rezervacije/ocene) ni search historiju:

```text
FinalScore = PopularityScore
```

Inače:

```text
FinalScore =
    PopularityWeight * PopularityScore +
    ContentWeight    * ContentScore +
    SearchWeight     * SearchScore
```

Sortiranje: `FinalScore` ↓, zatim `PopularityScore` ↓, zatim `SearchScore` ↓.

---

## 4. Objašnjive preporuke (`Reason`)

Svaka stavka odgovora sadrži ljudski čitljiv razlog, npr.:

- *Popular right now* (cold start)
- *Matches your interest in Sci-Fi*
- *Similar to movies you've enjoyed*
- *Matches your recent searches*
- *Popular with other viewers*
- *Already in your bookings*

Više razloga spaja se sa ` + `. Mobilna aplikacija prikazuje `reason` na kartici filma.

---

## 5. API i klijent

### Backend

```http
GET /Movies/Recommendations?take={n}
Authorization: Bearer {jwt}
```

- `take > 0` — vrati top N filmova  
- `take ≤ 0` (default query na kontroleru je `0`) — servis koristi default **10**  
- mobilni klijent šalje eksplicitni `take` (npr. 12) i povećava ga pri “load more”

Odgovor (`RecommendationResponse`):

- `movie` — podaci o filmu  
- `score` — finalni skor  
- `popularityScore`, `contentScore`, `searchScore`  
- `reason` — objašnjenje  

### Mobile

- Učitavanje: `MovieProvider.getRecommendations`  
- Upis pretrage: `POST /Movies/SearchHistory` pri filter/search akcijama  
- UI: `movies_page.dart` + `movie_card.dart` (prikaz razloga)

---

## 6. Konfiguracija

```env
Recommendations__PopularityWeight=0.4
Recommendations__ContentWeight=0.4
Recommendations__SearchWeight=0.2
```

Težine treba da budu u rasponu koji ima smisla za hibrid (zbir tipično 1.0). Promjena težina ne zahtijeva rebuild modela — recommender je **online / query-time** (nema zasebnog offline ML treninga).

---

## 7. Zašto ovaj pristup

| Pristup | Uloga u CineVision |
|---------|-------------------|
| Popularity | Rješava cold start i ističe popularan sadržaj |
| Content-based | Personalizacija prema žanru i sličnim opisima |
| Search history | Koristi stvarno ponašanje u app-u (obavezno po RS2 uputama) |
| Explainability | Korisnik vidi *zašto* je film predložen |

Nema collaborative filtering matrice korisnik–film; hibrid je namjerno jednostavan, determinističan i provjerljiv u odnosu na ovu dokumentaciju.
