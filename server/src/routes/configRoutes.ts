import { Router } from 'express';
import { authenticateToken, requirePermission } from '../middleware/auth';
import { getConfig, updateConfig } from '../controllers/configController';

const router = Router();

router.get('/', authenticateToken, getConfig);
router.post('/', authenticateToken, requirePermission('SYSTEM_CONFIG'), updateConfig);

export default router;
