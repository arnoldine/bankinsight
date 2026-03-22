import { Router } from 'express';
import { authenticateToken, requirePermission } from '../middleware/auth';
import {
    getAccounts,
    getAccountById,
    getAccountsByCustomerId,
    createAccount
} from '../controllers/accountController';

const router = Router();

router.get('/', authenticateToken, requirePermission('ACCOUNT_READ'), getAccounts);
router.get('/:id', authenticateToken, requirePermission('ACCOUNT_READ'), getAccountById);
router.get('/customer/:cif', authenticateToken, requirePermission('ACCOUNT_READ'), getAccountsByCustomerId);
router.post('/', authenticateToken, requirePermission('ACCOUNT_WRITE'), createAccount);

export default router;
