# 🎬 Cinema Booking Application

A desktop cinema booking and management application built with **C# and WPF**, featuring movie browsing, seat selection, user accounts, booking management, and administrative functionality.

> **Academic team project — 2 developers**  
> My primary contribution focused on the **frontend interface, application screens, and user navigation**.

## ✨ Features

### User Experience
- Browse available movies and view movie details
- Search for movies
- View movie sessions and showtimes
- Select seats for a cinema session
- Complete the booking flow through the payment interface
- View purchased tickets
- Create and manage a personal account
- Update profile information and profile picture
- Change or recover account password

### Administration
- Dedicated administrator login
- User management through an admin dashboard

## 🔐 Security

The project includes several authentication and security measures:

- User passwords are hashed using **PBKDF2 with SHA-256 and a random salt**
- Password hashes are compared using constant-time comparison
- Database operations use parameterized SQL queries where user input is supplied
- Administrator credentials are loaded from environment variables rather than stored directly in source code
- Local Visual Studio user configuration files are excluded from version control

## 🛠️ Tech Stack

- **C#**
- **.NET 8**
- **WPF / XAML**
- **MySQL**
- **MySql.Data**
- **MySqlConnector**

## 🏗️ Application Structure

The application is organized into separate WPF windows and pages for the main user flows:

- `CinemaMainWindow` — main cinema interface
- `MovieDetailsWindow` — movie information
- `MovieSessionCalendar` — session selection
- `SeatSelectionWindow` — cinema seat selection
- `PaymentWindow` — booking/payment interface
- `MyTickets` — user's tickets
- `MyProfile` — account management
- `SearchPage` — movie search
- `AdminLoginWindow` — administrator authentication
- `AdminDashboardWindow` — administration interface
- `DatabaseHelper` — database and user-account operations

## 🚀 Running the Project

### Requirements

- Windows
- .NET 8 SDK
- MySQL server
- Visual Studio with WPF/.NET desktop development support

### Setup

1. Clone the repository.
2. Configure a local MySQL database named `database_cinema`.
3. Update the local database connection settings if your MySQL configuration differs.
4. Configure administrator credentials through the environment variables:

```text
CINEMA_ADMIN_ID
CINEMA_ADMIN_KEY
```

5. Open `cinema_project.sln` in Visual Studio.
6. Restore the required NuGet packages and run the application.

## 👥 Project Context

This application was developed as an academic team project by two developers.

My main responsibility was the **frontend side of the application**, including the WPF/XAML interface, application screens, and navigation between user flows.

The project is included in my portfolio as an example of working with a multi-screen desktop application, collaborating on a shared codebase, and integrating a frontend interface with database-backed application functionality.

## 👤 Author

**Daniils Sergejevs**

## 📝 License

This project was created for educational purposes.
