
import { query, getClient } from '../db';
import fs from 'fs';
import path from 'path';

const initDb = async () => {
    const client = await getClient();
    try {
        const schemaPath = path.join(__dirname, '../db/schema.sql');
        const schemaSql = fs.readFileSync(schemaPath, 'utf8');

        console.log('Running Schema Initialization...');
        await client.query('BEGIN');
        await client.query(schemaSql);
        await client.query('COMMIT');
        console.log('Schema applied successfully.');
    } catch (e) {
        await client.query('ROLLBACK');
        console.error('Schema initialization failed:', e);
    } finally {
        client.release();
        process.exit();
    }
};

initDb();
