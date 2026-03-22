
import { DBProviderType } from '../types';

export interface IDatabaseProvider {
    getName(): string;
    generateSelect(table: string, limit?: number): string;
    generateInsert(table: string, data: Record<string, any>): string;
    generateUpdate(table: string, id: string, data: Record<string, any>): string;
    simulateLatency(): Promise<void>;
}

// --- MySQL Implementation ---
class MySQLProvider implements IDatabaseProvider {
    getName(): string { return 'MySQL 8.0 (InnoDB)'; }

    generateSelect(table: string, limit?: number): string {
        return `SELECT * FROM \`${table}\`${limit ? ` LIMIT ${limit}` : ''};`;
    }

    generateInsert(table: string, data: Record<string, any>): string {
        const keys = Object.keys(data).map(k => `\`${k}\``).join(', ');
        const values = Object.values(data).map(v => typeof v === 'string' ? `'${v}'` : v).join(', ');
        return `INSERT INTO \`${table}\` (${keys}) VALUES (${values});`;
    }

    generateUpdate(table: string, id: string, data: Record<string, any>): string {
        const sets = Object.entries(data).map(([k, v]) => `\`${k}\` = ${typeof v === 'string' ? `'${v}'` : v}`).join(', ');
        return `UPDATE \`${table}\` SET ${sets} WHERE \`id\` = '${id}';`;
    }

    async simulateLatency(): Promise<void> {
        await new Promise(resolve => setTimeout(resolve, 50)); // Fast
    }
}

// --- SQL Server Implementation ---
class SQLServerProvider implements IDatabaseProvider {
    getName(): string { return 'Microsoft SQL Server 2022'; }

    generateSelect(table: string, limit?: number): string {
        return `SELECT ${limit ? `TOP ${limit} ` : ''}* FROM [dbo].[${table}];`;
    }

    generateInsert(table: string, data: Record<string, any>): string {
        const keys = Object.keys(data).map(k => `[${k}]`).join(', ');
        const values = Object.values(data).map(v => typeof v === 'string' ? `N'${v}'` : v).join(', ');
        return `INSERT INTO [dbo].[${table}] (${keys}) VALUES (${values});`;
    }

    generateUpdate(table: string, id: string, data: Record<string, any>): string {
        const sets = Object.entries(data).map(([k, v]) => `[${k}] = ${typeof v === 'string' ? `N'${v}'` : v}`).join(', ');
        return `UPDATE [dbo].[${table}] SET ${sets} WHERE [id] = '${id}';`;
    }

    async simulateLatency(): Promise<void> {
        await new Promise(resolve => setTimeout(resolve, 120)); // Enterprise Network Simulation
    }
}

// --- PostgreSQL Implementation ---
class PostgreSQLProvider implements IDatabaseProvider {
    getName(): string { return 'PostgreSQL 15'; }

    generateSelect(table: string, limit?: number): string {
        return `SELECT * FROM "${table}"${limit ? ` LIMIT ${limit}` : ''};`;
    }

    generateInsert(table: string, data: Record<string, any>): string {
        const keys = Object.keys(data).map(k => `"${k}"`).join(', ');
        const values = Object.values(data).map((v, i) => `$${i+1}`).join(', '); // Simulate parameterized
        return `INSERT INTO "${table}" (${keys}) VALUES (${values}); -- Params: ${JSON.stringify(Object.values(data))}`;
    }

    generateUpdate(table: string, id: string, data: Record<string, any>): string {
        const sets = Object.entries(data).map(([k, v], i) => `"${k}" = $${i+1}`).join(', ');
        return `UPDATE "${table}" SET ${sets} WHERE "id" = '${id}';`;
    }

    async simulateLatency(): Promise<void> {
        await new Promise(resolve => setTimeout(resolve, 80)); 
    }
}

// --- Factory ---
export class DatabaseFactory {
    static getProvider(type: DBProviderType): IDatabaseProvider {
        switch (type) {
            case 'MYSQL': return new MySQLProvider();
            case 'SQLSERVER': return new SQLServerProvider();
            case 'POSTGRES': return new PostgreSQLProvider();
            default: return new MySQLProvider();
        }
    }
}
