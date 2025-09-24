# RecallAI Backend Deployment Guide

This guide covers deploying the RecallAI .NET backend to Render.com.

## 🚀 Quick Deploy to Render.com

### Prerequisites
- GitHub repository with your code
- Render.com account
- Supabase project with JWT secret

### Step 1: Push to GitHub
Ensure your code is pushed to GitHub with the deployment files:
- `Dockerfile`
- `render.yaml` 
- `appsettings.Production.json`
- `.dockerignore`

### Step 2: Create Render Web Service
1. Go to [Render.com](https://render.com) and sign in
2. Click **"New"** → **"Web Service"**
3. Connect your GitHub repository
4. Configure the service:
   - **Name**: `recallai-api`
   - **Region**: Choose closest to your users
   - **Branch**: `main` (or your preferred branch)
   - **Runtime**: `Docker`
   - **Dockerfile Path**: `./Dockerfile` (relative to repo root)

### Step 3: Configure Environment Variables
Add these environment variables in the Render dashboard:

#### Required Variables
```bash
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:$PORT
SUPABASE_URL=https://your-project.supabase.co
SUPABASE_JWT_SECRET=your-supabase-jwt-secret
SUPABASE_CONNECTION_STRING=postgresql://postgres:[password]@db.[project-ref].supabase.co:5432/postgres
```

#### Optional Variables
```bash
OPENAI_API_KEY=your-openai-api-key
FRONTEND_URL=https://your-frontend-domain.com
VERCEL_URL=your-vercel-app.vercel.app
```

### Step 4: Deploy
1. Click **"Create Web Service"**
2. Wait for the build and deployment to complete
3. Your API will be available at: `https://your-app-name.onrender.com`

## 🔧 Configuration Details

### Environment Variables Explained

| Variable | Description | Required |
|----------|-------------|----------|
| `ASPNETCORE_ENVIRONMENT` | Sets the .NET environment | Yes |
| `ASPNETCORE_URLS` | Configures the server URLs | Yes |
| `SUPABASE_URL` | Your Supabase project URL | Yes |
| `SUPABASE_JWT_SECRET` | JWT secret from Supabase settings | Yes |
| `SUPABASE_CONNECTION_STRING` | PostgreSQL connection string | Yes |
| `OPENAI_API_KEY` | OpenAI API key for AI features | No |
| `FRONTEND_URL` | Your frontend domain for CORS | No |
| `PORT` | Port number (auto-set by Render) | Auto |

### Getting Supabase Credentials

1. **Supabase URL**: Go to Settings → API → Project URL
2. **JWT Secret**: Go to Settings → API → JWT Secret
3. **Connection String**: Go to Settings → Database → Connection string

## 🏥 Health Check

Your deployed API includes a health check endpoint:
- **URL**: `https://your-app-name.onrender.com/api/health`
- **Method**: `GET`
- **Response**: JSON with health status

## 📊 Monitoring

### Render Dashboard
- View logs in real-time
- Monitor CPU and memory usage
- Check deployment history
- Manage environment variables

### Swagger Documentation
Available at: `https://your-app-name.onrender.com/swagger` (production disabled by default)

## 🔒 Security Notes

- JWT secrets are automatically loaded from environment variables
- HTTPS is enabled by default on Render
- CORS is configured for your frontend domains
- Database connection strings use SSL

## 🚨 Troubleshooting

### Common Issues

1. **Build Fails**
   - Check Dockerfile syntax
   - Ensure all dependencies are restored
   - Verify .csproj file is correct

2. **Database Connection Issues**
   - Verify connection string format
   - Check Supabase database is running
   - Ensure PostgreSQL extension is enabled

3. **JWT Authentication Fails**
   - Verify JWT secret matches Supabase
   - Check token format in frontend
   - Ensure middleware is configured correctly

4. **CORS Errors**
   - Add your frontend domain to environment variables
   - Check FRONTEND_URL or VERCEL_URL settings
   - Verify CORS policy in Program.cs

### Logs and Debugging

View logs in the Render dashboard:
1. Go to your service
2. Click the **"Logs"** tab
3. Monitor real-time application logs

## 🔄 Updates and Redeployment

Render automatically redeploys when you push to your configured branch:
1. Make changes to your code
2. Commit and push to GitHub
3. Render will automatically build and deploy

## 💰 Pricing

- **Free Tier**: 750 hours/month, sleeps after 15 minutes
- **Starter**: $7/month, always on, custom domains
- **Professional**: $25/month, more resources

Your API URL will be: `https://recallai-api.onrender.com`