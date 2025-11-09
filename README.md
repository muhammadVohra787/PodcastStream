# Podcast Streamer

A **full-stack web application** for managing and streaming podcasts. Users can **browse, listen, and download episodes**, while Podcasters can **upload, edit, and manage podcasts**. Includes **role-based access control** with Admin, Podcaster, and Listener roles.

## Features

* **Role-Based Access Control:** Admin, Podcaster, and Listener roles with different permissions.
* **Podcast Management:** Podcasters can upload audio, edit episodes, and view dashboards.
* **Streaming & Downloads:** Users can play episodes directly in the browser or download them.
* **AWS S3 Integration:** Audio files are securely uploaded and stored in S3.
* **Responsive UI:** Clean, mobile-friendly interface with optional dark mode.

## Tech Stack

* **Frontend:** Blazor, ASP.NET Core MVC, Bootstrap
* **Backend:** ASP.NET Core, C#
* **Database:** SQL Server / Entity Framework Core
* **Cloud Storage:** AWS S3
* **Authentication & Authorization:** ASP.NET Identity, role-based access

## Getting Started

1. **Clone the repository:**

   ```bash
   git clone https://github.com/muhammadVohra787/PodcastStreamer.git
   ```

2. **Configure the database** connection string and **AWS Secret key and Access Key** in `appsettings.json`.

3. **Run migrations:**

   ```bash
   dotnet ef database update
   ```

4. **Configure AWS S3** credentials in your app settings.

5. **Run the application:**

   ```bash
   dotnet run
   ```

6. **Access the application:**
   Visit `https://localhost:5001` (or configured port) in your browser.

## Usage

* Register or log in.
* Listeners can browse and stream episodes.
* Podcasters can create podcasts, upload episodes, and manage content.
* Admins can manage users and oversee platform activity.

