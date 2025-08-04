from fastapi import FastAPI, Depends, HTTPException, status
from fastapi.middleware.cors import CORSMiddleware
import uvicorn

app = FastAPI(
    title="AI_BACKUP API",
    description="API for AI_BACKUP backup solution",
    version="0.1.0",
)

# Configure CORS
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # In production, replace with specific origins
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

@app.get("/")
async def root():
    return {"message": "Welcome to AI_BACKUP API"}

@app.get("/health")
async def health_check():
    return {"status": "healthy"}

# Import and include routers
# from .routers import users, backups, clients, storage, licenses

# app.include_router(users.router)
# app.include_router(backups.router)
# app.include_router(clients.router)
# app.include_router(storage.router)
# app.include_router(licenses.router)

if __name__ == "__main__":
    uvicorn.run("main:app", host="0.0.0.0", port=12000, reload=True)