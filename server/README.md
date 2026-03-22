# BankInsight Backend

Node.js + Express + PostgreSQL backend for BankInsight.

## Setup

1.  **Install Dependencies:**
    ```bash
    npm install
    ```

2.  **Database Setup:**
    -   Ensure PostgreSQL is running.
    -   Create a database named `bankinsight`.
    -   Copy `.env.example` to `.env` and update credentials.

3.  **Initialize Database:**
    -   Run the schema script in your SQL tool or via:
    ```bash
    npm run db:init
    ```
    (Note: You might need to configure a script for this or run `psql -d bankinsight -f db/schema.sql`)

4.  **Seed Data:**
    ```bash
    npm run db:seed
    ```

5.  **Run Server:**
    ```bash
    npm run dev
    ```

## API Endpoints
-   `POST /api/auth/login`: Login
