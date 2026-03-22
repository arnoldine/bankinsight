import { Router } from 'express';
import { getGlAccounts, createGlAccount, getJournalEntries, postJournalEntry } from '../controllers/glController';

const router = Router();

router.get('/accounts', getGlAccounts);
router.post('/accounts', createGlAccount);
router.get('/journals', getJournalEntries);
router.post('/journals', postJournalEntry);

export default router;
