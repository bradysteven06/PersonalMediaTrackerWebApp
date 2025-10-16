# Personal Media Tracker Web App

A full-stack web application for tracking and organizing your favorite media — including anime, movies, TV shows, and more.  
Built with ASP.NET Core 8, Entity Framework Core, and JavaScript (ES6), this project demonstrates secure CRUD operations, user authentication, and a clean, responsive UI.

---

## Features

### Core Functionality
- Add, edit, and delete media entries  
- Track title, type, subtype, genres, status, rating, and notes  
- Filtered and formatted entry list display  
- Dark mode toggle and “Stay on this page” pill-style toggle  
- Responsive layout with modern styling  

### Authentication
- Secure user registration and login with ASP.NET Identity  
- Each user has a private, personalized media list  
- Protected routes — only authenticated users can modify data  
- Automatic redirect to login page when unauthorized  

### Backend API
- RESTful API built with ASP.NET Core Web API  
- Entity Framework Core for database access  
- DTO mapping between client and database  
- Proper model validation and error handling  

### Frontend
- Vanilla JavaScript (ES6 modules)  
- Fetch-based API calls (GET, POST, PUT, DELETE)  
- Clean, intuitive layout using custom `styles.css`  
- Dynamic tag/genre system with clickable pill toggles  

---

## Tech Stack

| Layer           | Technology                                                          |
|-----------------|---------------------------------------------------------------------|
| Frontend        | HTML5, CSS3, JavaScript (ES6 Modules)                               |
| Backend         | ASP.NET Core 8 Web API                                              |
| Database        | Entity Framework Core + SQLite                                      |        
| Authentication  | ASP.NET Identity (JWT or Cookie Auth)                               |
| Architecture    | Multi-Project Clean Architecture (Domain, Infrastructure, WebApi)   |
| Version Control | Git + GitHub                                                        |

---

## Project Structure

```
PersonalMediaTrackerWebApp/
├── backend/
│   ├── Domain/                # Entities and Enums
│   ├── Infrastructure/        # EF Core setup, DbContext, Mappings
│   ├── WebApi/                # Controllers, Startup, Identity
│   └── appsettings.json
├── frontend/
│   ├── index.html
│   ├── entry.html
│   ├── login.html
│   ├── scripts/
│   │   ├── app.js
│   │   ├── entry.js
│   │   ├── api.js
│   │   └── auth.js
│   └── styles/
│       └── styles.css
└── README.md
```

---

## Setup Instructions

### Prerequisites
- [.NET SDK 8.0+](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (optional, if you add npm tooling later)
- SQLite (included with EF Core)

### 1. Clone the Repository
```bash
git clone https://github.com/bradysteven06/PersonalMediaTrackerWebApp.git
cd PersonalMediaTrackerWebApp
```

### 2. Set Up the Backend
```bash
cd backend/WebApi
dotnet restore
dotnet ef database update
dotnet run
```
The API should now be running at:
```
https://localhost:5001
```

### 3. Run the Frontend
Open `frontend/index.html` in your browser.  
(Ensure CORS allows your frontend origin — already configured in `Program.cs`.)

### 4. Register and Log In
1. Open `login.html`
2. Create a new user account
3. Log in and start adding entries — they’ll be stored per user

---

## Entity Model Overview

| Field  | Type             | Description                           |
|--------|------------------|---------------------------------------|
| Id     | int              | Auto-generated unique identifier      |
| Title  | string           | Media title                           |
| Type   | EntryType enum   | e.g., Series, Movie, Anime            |
| SubType| EntrySubType?    | e.g., Live Action, Animated           |
| Genres | List<string>     | Tags stored as a delimited string     |
| Status | EntryStatus enum | Watching, Completed, Dropped, etc.    |
| Rating | float            | User rating (supports half-points)    |
| Notes  | string?          | Optional personal notes               |
| UserId | string           | Identity user foreign key             |

---

## Future Roadmap

- Lock/clear SubType when Type changes
- Expand available types/subtypes/genres   
- Entry count indicator  
- Replace `confirm()` with custom modal  
- Add toast messages (success/error/undo)  
- Implement “Undo Delete” buffer  
 

---

## Screenshots (Placeholders)

| Login Page                                      | Media List                                    | Add/Edit Entry                                |
|-------------------------------------------------|-----------------------------------------------|-----------------------------------------------|
| ![Login Screenshot](docs/screenshots/login.png) | ![List Screenshot](docs/screenshots/list.png) | ![Form Screenshot](docs/screenshots/form.png) |


---

## Lessons and Highlights

- Implemented Clean Architecture with clear separation of concerns  
- Mastered Entity Framework Core migrations and data relationships  
- Integrated ASP.NET Identity for real user management  
- Built modular JavaScript frontend with reusable API layer  
- Practiced responsive and accessible front-end design principles  

---

## License
This project is released under the [MIT License](LICENSE).

---

## Author
**Steven Brady**  
[GitHub: @bradysteven06](https://github.com/bradysteven06)

If you found this project useful, consider giving it a star on GitHub.
