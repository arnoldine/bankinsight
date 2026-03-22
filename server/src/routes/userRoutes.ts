import { Router } from 'express';
import { authenticateToken, requirePermission } from '../middleware/auth';
import {
    getUsers,
    getUserById,
    createUser,
    updateUser,
    deleteUser
} from '../controllers/userController';

const router = Router();

router.get('/', authenticateToken, requirePermission('SYSTEM_ADMIN'), getUsers);
router.get('/:id', authenticateToken, getUserById);
router.post('/', authenticateToken, requirePermission('SYSTEM_ADMIN'), createUser);
router.put('/:id', authenticateToken, requirePermission('SYSTEM_ADMIN'), updateUser);
router.delete('/:id', authenticateToken, requirePermission('SYSTEM_ADMIN'), deleteUser);

export default router;
