import { Router } from 'express';
import { authenticateToken, requirePermission } from '../middleware/auth';
import { getApprovals, createApproval, updateApproval } from '../controllers/approvalController';

const router = Router();

router.get('/', authenticateToken, requirePermission('LOAN_APPROVE'), getApprovals);
router.post('/', authenticateToken, createApproval);
router.put('/:id', authenticateToken, requirePermission('LOAN_APPROVE'), updateApproval);

export default router;
