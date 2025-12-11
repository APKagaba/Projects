# 📁 File & Directory Browser

A modern, full-stack file management application built with **ASP.NET Core 8** (backend) and **Vanilla JavaScript** (frontend). 

Browse, search, upload, download, and delete files and folders directly from your browser with a clean, responsive interface.

**Live Demo:** https://github.com/APKagaba/Projects

---

## 🎯 How It Works

### Frontend (index.html)
The application features a single-page interface built with vanilla JavaScript that communicates with the backend via REST API calls. Key features include:

- **Breadcrumb Navigation**: Click on path segments to navigate between directories
- **File Browsing**: View all files and folders in the current directory with file sizes
- **Search**: Real-time search across files and folders with results displayed as you type
- **Upload**: Upload individual files or entire folder structures with support for multiple files
- **Download**: Download files to your local machine
- **Delete**: Remove files and folders (with confirmation prompt)
- **Statistics**: View folder count, file count, and total size of current directory

The frontend maintains state for the current path (via URL hash) and handles all user interactions with visual feedback including loading indicators, success/error messages, and empty states.

### Backend (TestController.cs)
The ASP.NET Core controller provides a secure REST API with the following endpoints:

#### Browse Endpoint
- **GET** `/api/test/browse?path={path}`
- Lists all directories and files in the specified path
- Returns directory names, file names, and file sizes
- Results are sorted alphabetically
- Includes security checks to prevent directory traversal attacks

#### Search Endpoint
- **GET** `/api/test/search?query={query}&path={path}`
- Recursively searches for files and folders matching the query string
- Returns matching items with their full paths and file sizes
- Supports case-insensitive search across the entire directory tree
- Handles access denied errors gracefully

#### Upload Endpoint
- **POST** `/api/test/upload?path={path}`
- Accepts multiple files in a single request using multipart form data
- Creates nested directories automatically if needed
- Returns detailed upload statistics (successful and failed uploads)
- Includes security validation to prevent path traversal

#### Download Endpoint
- **GET** `/api/test/download?path={path}`
- Serves the specified file for download to the client
- Validates path security before serving

#### Delete Endpoint
- **DELETE** `/api/test/delete?path={path}&isDirectory={true/false}`
- Deletes individual files or entire directories (recursively)
- Includes confirmation on the frontend before execution
- Validates path security

### Security Features
- **Path Validation**: All requests validate that paths remain within the configured home directory
- **Path Traversal Protection**: Prevents attempts to access directories outside the allowed root
- **Comprehensive Error Handling**: Graceful error messages for invalid paths, missing items, and server errors
- **Logging**: Server-side logging of operations and errors for debugging

---

## 🎯 Quick Start

### Prerequisites
- .NET 8 SDK or later
- Visual Studio 2022+ or VS Code with C# extension
- Modern web browser (Chrome, Firefox, Safari, Edge)

### Installation & Running

1. **Clone the repository**
```bash
git clone https://github.com/APKagaba/Projects.git
cd Projects/TestProject
```

2. **Build and run the application**
```bash
dotnet build
dotnet run
```

3. **Access the application**
- Open your browser and navigate to `http://localhost:5000`
- The application will display the file browser interface

---

## 📋 Features in Detail

### Navigation
- Use the breadcrumb buttons at the top to navigate between directories
- Click on any folder in the list to open it
- The URL hash updates automatically for bookmarking

### Search
- Type in the search box to filter files and folders in real-time
- Search is case-insensitive and works recursively
- View the raw API response by clicking "View API Response"

### File Operations
- **Upload**: Click the "Upload" button to upload files or folders
- **Download**: Click the download button next to any file
- **Delete**: Click the delete button to remove files or folders (confirmation required)

---

## 🔒 Security

The application implements several security measures:

- **Path validation** on every request to prevent directory traversal attacks
- **Relative path enforcement** to keep operations within the designated directory
- **Server-side logging** of all operations and errors
- **Graceful error handling** without exposing sensitive information

---

## 📝 Configuration

The application can be configured via `appsettings.json`:

```json
{
  "FileServer": {
    "HomeDirectory": "wwwroot"
  }
}
```

The `HomeDirectory` setting specifies the root directory for file operations (defaults to `wwwroot` if not specified).
