import { Router } from 'express';
import { getLoans, disburseLoan } from '../controllers/loanController';

const router = Router();

router.get('/', getLoans);
router.post('/disburse', disburseLoan);

export default router;
