const { Client } = require('pg');
const client = new Client("postgresql://postgres:postgres@localhost:5432/bankinsight");
client.connect().then(async () => {
    try {
        const res = await client.query(`
            SELECT p.id, p.code, p.name 
            FROM role_permissions rp 
            JOIN permissions p ON p.id = rp.permission_id 
            WHERE rp.role_id = 'ROLE_ADMIN' 
            LIMIT 5
        `);
        console.log("Joined Permissions:");
        console.log(res.rows);
    } catch(e) { console.error(e.message); }
    process.exit();
});
