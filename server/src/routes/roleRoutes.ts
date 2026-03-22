import { Router } from 'express';
import { authenticateToken, requirePermission } from '../middleware/auth';
import {
    getRoles,
    createRole,
    updateRole
} from '../controllers/roleController';

const router = Router();

router.get('/', authenticateToken, requirePermission('SYSTEM_ADMIN'), getRoles);
router.post('/', authenticateToken, requirePermission('SYSTEM_ADMIN'), createRole);
router.put('/:id', authenticateToken, requirePermission('SYSTEM_ADMIN'), updateRole);

export default router;
