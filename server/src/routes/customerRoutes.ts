import { Router } from 'express';
import { authenticateToken, requirePermission } from '../middleware/auth';
import {
    getCustomers,
    getCustomerById,
    createCustomer,
    updateCustomer
} from '../controllers/customerController';

const router = Router();

router.get('/', authenticateToken, requirePermission('ACCOUNT_READ'), getCustomers);
router.get('/:id', authenticateToken, requirePermission('ACCOUNT_READ'), getCustomerById);
router.post('/', authenticateToken, requirePermission('ACCOUNT_WRITE'), createCustomer);
router.put('/:id', authenticateToken, requirePermission('ACCOUNT_WRITE'), updateCustomer);

export default router;
