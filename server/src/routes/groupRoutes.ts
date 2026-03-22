import { Router } from 'express';
import { getGroups, createGroup, addMember, removeMember } from '../controllers/groupController';

const router = Router();

router.get('/', getGroups);
router.post('/', createGroup);
router.post('/:id/members', addMember);
router.delete('/:id/members/:customerId', removeMember);

export default router;
