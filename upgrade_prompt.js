const fs = require('fs');
const path = require('path');

async function processPrompt() {
    try {
        const filePath = path.join(__dirname, 'prompt.md');
        let content = fs.readFileSync(filePath, 'utf8');

        // 1. Generic database terminology replacements
        content = content.replace(/OpenInsight \[RevG\] \/ /g, '');
        content = content.replace(/OpenInsight \(RevG\) \/ /g, '');
        content = content.replace(/OpenInsight/gi, 'PostgreSQL');
        content = content.replace(/Linear Hash database environment/gi, 'Relational database environment');
        content = content.replace(/MultiValue \(NoSQL\)/gi, 'Relational (SQL)');
        content = content.replace(/Basic\+/gi, 'PL/pgSQL');

        // 2. Inject Antigravity IDE instructions at the top
        content = content.replace(
            /(# BankInsight System Prompt)/,
            `$1\n\n> [!NOTE]\n> **Target Environment**: This prompt is designed for the **Antigravity IDE**. Ensure all architectural components are built around PostgreSQL and standard modern relational database patterns.`
        );

        // 3. Clear ALL mock arrays except STAFF
        const arraysToEmpty = [
            'BRANCHES', 'CUSTOMERS', 'GROUPS', 'PRODUCTS', 'ACCOUNTS', 'LOANS',
            'GL_ACCOUNTS', 'JOURNAL_ENTRIES', 'COMPLIANCE_METRICS', 'AUDIT_LOGS',
            'DEV_TASKS', 'APPROVAL_REQUESTS', 'ROLES', 'WORKFLOWS'
        ];

        arraysToEmpty.forEach(arrName => {
            const regex = new RegExp(`export const ${arrName}:.*?= \\[([\\s\\S]*?)\\];`, 'm');
            content = content.replace(regex, `export const ${arrName}: any[] = [];`);
        });

        // 4. Leave only Admin User in STAFF array
        const staffRegex = /export const STAFF: StaffUser\[\] = \[([\s\S]*?)\];/m;
        content = content.replace(staffRegex, `export const STAFF: StaffUser[] = [
    { id: 'STF001', name: 'Admin User', roleId: 'R001', roleName: 'Super Administrator', branchId: 'BR001', email: 'admin@bankinsight.local', phone: '0200000001', avatarInitials: 'AD', status: 'Active', lastLogin: new Date().toLocaleString(), password: 'password' }
];`);

        // 5. Add "User Management" page instructions
        // We'll insert it right after the App.tsx definition in the prompt
        if (content.indexOf('### App.tsx') !== -1) {
            const userManagementComponent = `

### components/UserManagement.tsx
\`\`\`tsx
import React, { useState } from 'react';
import { User, Shield, ShieldCheck, Mail, Phone, Lock, Eye, EyeOff, Building, MoreVertical, Edit2, Trash2 } from 'lucide-react';
import { StaffUser } from '../types';

interface UserManagementProps {
    users: StaffUser[];
}

const UserManagement: React.FC<UserManagementProps> = ({ users }) => {
    const [searchTerm, setSearchTerm] = useState('');
    
    return (
        <div className="p-6">
            <h1 className="text-2xl font-bold mb-6">User Management</h1>
            <div className="bg-white rounded-xl shadow border border-gray-100 p-6">
                 Here you will implement a data table showing all staff members, their roles, and branch assignment.
                 Include forms/modals to Create, Update, and Delete users.
                 Link new user passwords to the AuthProvider / Login Page so they can authenticate.
            </div>
        </div>
    );
};

export default UserManagement;
\`\`\`
`;
            // Insert before the next ### header after App.tsx
            const restOfContent = content.substring(content.indexOf('### App.tsx'));
            const nextHeaderPos = restOfContent.indexOf('\n### ', 12);
            if (nextHeaderPos !== -1) {
                const absolutePos = content.indexOf('### App.tsx') + nextHeaderPos;
                content = content.substring(0, absolutePos) + userManagementComponent + content.substring(absolutePos);
            }
        }

        // 6. Write to output file
        const outputPath = path.join(__dirname, 'prompt-antigravity.md');
        fs.writeFileSync(outputPath, content, 'utf8');
        console.log('Successfully generated prompt-antigravity.md');
        console.log('Total length:', content.length);

    } catch (err) {
        console.error('Error processing prompt:', err);
    }
}

processPrompt();
