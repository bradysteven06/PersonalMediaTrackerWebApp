[![.NET Build & Test](https://github.com/bradysteven06/PersonalMediaTrackerWebApp/actions/workflows/dotnet.yml/badge.svg)](https://github.com/bradysteven06/PersonalMediaTrackerWebApp/actions/workflows/dotnet.yml)

# Personal Media Tracker Web App

A full-stack web application that lets users track and organize anime, manga, movies, and TV shows.  
Built with **ASP.NET Core 8**, **Entity Framework Core**, and **JavaScript (ES6 Modules)**, this project demonstrates secure authentication, CRUD operations, validation, and a clean responsive design.

---

## Features

### Core Functionality
- Add, edit, and delete media entries
- Track title, type, subtype, genres, status, rating, and notes
- Filter and sort by type, genre, or rating
- Responsive layout with light/dark mode support
- “Stay on this page” pill toggle for rapid entry
- Soft-delete implementation for safe data removal

### Authentication
- ASP.NET Identity integration for user registration and login
- JWT-based authentication
- Per-user data isolation
- Automatic redirect to login page when unauthorized

### Backend API
- RESTful ASP.NET Core Web API
- Entity Framework Core with SQL Server
- DTO mapping between entities and API contracts
- Validation for rating (0–10, 0.5 increments)
- TagSync service for many-to-many tag management

### Frontend
- Vanilla JavaScript (ES6 Modules)
- Fetch-based API layer with unified error handling
- LocalStorage for theme preference (dark mode)
- Reusable pill-style genre toggles
- Clean, accessible, mobile-first UI

---

## Tech Stack

| Layer             | Technology                                                        |
|-------------------|-------------------------------------------------------------------|
| Frontend          | HTML5, CSS3, JavaScript (ES6 Modules)                             |
| Backend           | ASP.NET Core 8 Web API                                            |
| Database          | Entity Framework Core + SQL Server                                |
| Authentication    | ASP.NET Identity + JWT                                            |
| Testing           | xUnit, FluentAssertions, Moq, EF Core InMemory, SQLite In-Memory  |
| Version Control   | Git + GitHub                                                      |

---

## Project Structure

```
PersonalMediaTrackerWebApp/
├── backend/
│   ├── Domain/
│   ├── Infrastructure/
│   ├── WebApi/
│   ├── Tests/
│   └── Program.cs
├── frontend/
│   ├── index.html
│   ├── entry.html
│   ├── login.html
│   ├── scripts/
│   │   ├── app.js
│   │   ├── entry.js
│   │   ├── api.js
│   │   ├── auth.js
│   │   └── enums.js
│   ├── styles/
│   │   └── styles.css
│   └── docs/
│       └── screenshots/
│           ├── login.png
│           ├── list.png
│           ├── form.png
│           ├── loginDark.png
│           ├── listDark.png
│           └── formDark.png
└── README.md
```

---

## Setup Instructions

### Prerequisites
- .NET SDK 8.0+
- SQL Server
- Node.js (optional, for frontend tooling)

### 1. Clone the Repository
```bash
git clone https://github.com/bradysteven06/PersonalMediaTrackerWebApp.git
cd PersonalMediaTrackerWebApp
```

### 2. Configure and Run the Backend
```bash
cd backend/WebApi
dotnet restore
dotnet ef database update
dotnet run
```
API runs by default at `https://localhost:7106`.

### 3. Launch the Frontend
Opening `frontend/index.html` directly (via `file://`) **will not work**, browsers block API requests from local files to `https://localhost`.  
You’ll need to serve it using **VS Code Live Server** or another lightweight static server.

#### Recommended: Using VS Code “Live Server”
1. Open the repository folder in **VS Code**.  
2. Right-click `frontend/index.html` → **Open with Live Server**.  
   - This usually runs at `http://127.0.0.1:5500` or `http://localhost:5500`.  
3. In `frontend/api.js`, make sure your backend URL matches your WebApi’s HTTPS address:
   ```js
   // api.js
   const BASE_URL = "https://localhost:7106"; // use your backend’s actual port
   ```
4. Ensure your WebApi CORS policy allows these origins in `Program.cs`:
   ```csharp
   builder.Services.AddCors(o => o.AddPolicy("Dev",
       p => p.WithOrigins("http://127.0.0.1:5500", "http://localhost:5500")
             .AllowAnyHeader()
             .AllowAnyMethod()));
   app.UseCors("Dev");
   ```
   *(This is already configured in your code, but confirm if you use a different port.)*

Both should now be running:
- API -> `https://localhost:7106`  
- Frontend -> `http://127.0.0.1:5500`  

### 4. Usage
1. Register a user account on `login.html`
2. Log in to access your personal media list
3. Add, edit, and delete entries as desired

---

## Entity Model Overview

| Field     | Type              | Description                           |
|-----------|-------------------|---------------------------------------|
| Id        | Guid              | Primary key                           |
| UserId    | Guid              | Owner user ID                         |
| Title     | string            | Media title (required)                |
| Type      | EntryType         | Movie / Series                        |
| SubType   | EntrySubType?     | LiveAction, Anime, etc.               |
| Status    | EntryStatus       | Planning, Watching, Completed, etc.   |
| Rating    | decimal?          | 0–10 in 0.5 increments                |
| Notes     | string?           | Optional user notes                   |
| Tags      | ICollection<Tag>  | Many-to-many genre tags               |

---

## Testing Summary

Unit and integration tests verify all critical components:

| Category              | Focus                                                         |
|-----------------------|---------------------------------------------------------------|
| Domain Tests          | Mapping logic, validation, enum conversion                    |
| Infrastructure Tests  | EF Core model configuration, soft-delete filters, timestamps  |
| Controller Tests      | CRUD operations, validation responses, tag syncing            |
| Service Tests         | JWT token generation, TagSyncService behavior                 |
| Integration Tests     | End-to-end API flow using WebAppFactoryFixture                |

All tests pass successfully via:
```bash
dotnet test
```

---

## Future Roadmap

- Lock/clear SubType when Type changes
- Show total entry count in media list
- Add more types, subtypes, and genre tags
- Replace confirm() with a custom modal
- Add toast notifications for actions
- Allow undo of deleted entries (temporary buffer)

---

## Screenshots

| Login                                 | List                                  | Add/Edit                           |
|---------------------------------------|---------------------------------------|------------------------------------|
| ![Login](docs/images/login.png)       | ![List](docs/images/list.png)         | ![Form](docs/images/form.png)      |
|                                       | ![List](docs/images/listDark.png)     | ![Form](docs/images/formDark.png)  |

---

## Lessons and Highlights

- Implemented clean architecture principles across backend layers  
- Designed and tested EF Core soft-delete behavior  
- Created responsive frontend with modular ES6 JavaScript  
- Practiced DTO and mapping validation for API consistency  
- Built robust test suite for maintainability and reliability

---

## License

This project is licensed under the [MIT License](LICENSE).

---

## Author

**Steven Brady**  
[GitHub: @bradysteven06](https://github.com/bradysteven06)

