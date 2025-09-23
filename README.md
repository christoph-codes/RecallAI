# RecallAI
AI Memory for everyone

## Project Structure

This repository contains two main components:

### Frontend (`/frontend`)
- **Technology**: Next.js 15 + TypeScript + Tailwind CSS
- **Framework**: React with App Router
- **Styling**: Tailwind CSS for utility-first styling
- **Deployment**: Configured for Vercel

### Backend (`/backend`)
- **Technology**: .NET 8 Web API
- **Framework**: ASP.NET Core
- **Language**: C#
- **Deployment**: Configured for Render.com

## Getting Started

### Frontend Development
```bash
cd frontend
npm install
npm run dev
```

### Backend Development
```bash
cd backend/RecallAI.Api
dotnet restore
dotnet run
```

## Building for Production

### Frontend
```bash
cd frontend
npm run build
```

### Backend
```bash
cd backend/RecallAI.Api
dotnet build --configuration Release
```

## Project Features

- **Frontend**: Modern React application with TypeScript for type safety and Tailwind for responsive design
- **Backend**: RESTful API built with .NET 8 for high performance and scalability
- **Deployment Ready**: Both projects include appropriate configurations for their respective hosting platforms
