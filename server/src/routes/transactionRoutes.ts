import { Router } from 'express';
import {
    getTransactions,
    getTransactionById,
    postTransaction
} from '../controllers/transactionController';

const router = Router();

router.get('/', getTransactions);
router.get('/:id', getTransactionById);
router.post('/', postTransaction);

export default router;
