import express from 'express';
import cors from 'cors';
import dotenv from 'dotenv';
import authRoutes from './routes/authRoutes';
import userRoutes from './routes/userRoutes';
import customerRoutes from './routes/customerRoutes';
import accountRoutes from './routes/accountRoutes';
import transactionRoutes from './routes/transactionRoutes';
import productRoutes from './routes/productRoutes';
import glRoutes from './routes/glRoutes';
import groupRoutes from './routes/groupRoutes';
import loanRoutes from './routes/loanRoutes';
import roleRoutes from './routes/roleRoutes';
import configRoutes from './routes/configRoutes';
import workflowRoutes from './routes/workflowRoutes';
import approvalRoutes from './routes/approvalRoutes';
dotenv.config();

const app = express();
const port = parseInt(process.env.PORT || '3001', 10);

app.use(cors());
app.use(express.json());

app.get('/health', (req, res) => {
    res.json({ status: 'ok', timestamp: new Date().toISOString() });
});

app.use('/api/auth', authRoutes);
app.use('/api/users', userRoutes);
app.use('/api/customers', customerRoutes);
app.use('/api/accounts', accountRoutes);
app.use('/api/transactions', transactionRoutes);
app.use('/api/products', productRoutes);
app.use('/api/gl', glRoutes);
app.use('/api/groups', groupRoutes);
app.use('/api/loans', loanRoutes);
app.use('/api/roles', roleRoutes);
app.use('/api/config', configRoutes);
app.use('/api/workflows', workflowRoutes);
app.use('/api/approvals', approvalRoutes);

app.listen(port, () => {
    console.log(`Server running at http://localhost:${port}`);
});
