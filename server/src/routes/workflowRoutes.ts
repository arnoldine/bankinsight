import { Router } from 'express';
import { getWorkflows, createWorkflow, updateWorkflow } from '../controllers/workflowController';

const router = Router();

router.get('/', getWorkflows);
router.post('/', createWorkflow);
router.put('/:id', updateWorkflow);

export default router;
