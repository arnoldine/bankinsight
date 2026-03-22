fetch('http://localhost:5176/api/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email: 'admin@bankinsight.local', password: 'password123' })
}).then(r => r.json()).then(d => {
    if (d.user) {
        console.log("Permissions count:", d.user.permissions.length);
        console.log("Permissions sample:", d.user.permissions.slice(0, 5));
    } else {
        console.log("Login failed", d);
    }
}).catch(console.error);
