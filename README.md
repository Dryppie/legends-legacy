# legends-legacy

## Backend Requirements:

- Visual Studio (Recommended)
- .NET 10 SDK
- MSSQL Express
- Azure Data Studio (Recommended tool for managing Database)

## Frontend Requirements:

- Node.js (Latest LTS version recommended)
- Angular CLI (npm install -g @angular/cli)
- VSCode (Recommended)

## Guide

### Install Dependencies

Ensure all required software is installed before proceeding.

### Backend Setup

Open the backend solution found at /legends-legacy/LL/LegendsLegacy.sln project in Visual Studio.

#### Install DOTNET and Apply migrations:

Run the following to install dotnet.<br>
This can be done from your Package Manager Console, found in VS.<br>
`dotnet tool install --global dotnet-ef`

Then run the following to apply the latest migrations and set up the database<br>
`dotnet ef database update`

Verify database connection using Azure Data Studio (Recommended) or SQL Server Management Studio.

### Frontend Setup

Navigate to the frontend directory at /legends-legacy/LL/src/Presentation/ll/

Open VSCode from this path.<br>
Open a new terminal directly in VSCode

#### Install dependencies:

`npm install`

#### Start the development server:

`ng serve -o`
