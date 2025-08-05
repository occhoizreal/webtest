# ChatPlay

A Vue.js application with a C# backend for document processing and chat functionality.

## Project Structure

- `frontend/` - Vue.js frontend application built with Vite
- `backend/` - C# ASP.NET Core backend application

## GitHub Pages Deployment

This project is configured to automatically deploy the frontend to GitHub Pages when changes are pushed to the `main` branch.

### Setup Instructions

1. **Enable GitHub Pages in your repository settings:**
   - Go to Settings → Pages
   - Under "Source", select "GitHub Actions"

2. **The deployment workflow will automatically:**
   - Build the Vue.js frontend
   - Deploy it to GitHub Pages
   - Make it available at `https://[username].github.io/webtest/`

### Manual Deployment

You can also trigger a manual deployment by:
- Going to the "Actions" tab in your GitHub repository
- Selecting the "Deploy to GitHub Pages" workflow
- Clicking "Run workflow"

### Local Development

To run the frontend locally:

```bash
cd frontend
npm install
npm run dev
```

The application will be available at `http://localhost:5173`

## Features

- Document processing and chunking
- Intelligent text analysis
- Vue.js frontend with modern UI
- C# backend API