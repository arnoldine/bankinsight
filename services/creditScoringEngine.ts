
import { CreditScore, Financials } from '../types';

/**
 * BANKINSIGHT CREDIT SCORING ENGINE (v1.2)
 * 
 * This service mimics a backend microservice or OpenInsight Stored Procedure
 * that calculates credit risk based on financial inputs.
 * 
 * Logic based on standard lending metrics:
 * 1. Debt-To-Income (DTI)
 * 2. Payment-To-Income (PTI)
 * 3. Net Worth Check
 * 4. KYC Regulatory Limits
 * 5. Principal Burden (Loan Amount / Term)
 */

export const calculateCreditScore = (
    financials: Financials, 
    loanAmount: number, 
    loanTermMonths: number, 
    interestRate: number,
    kycLevel: string = 'Tier 1' // Default to lowest risk bucket if unknown
): CreditScore => {
    let score = 100; // Start perfect
    const factors: string[] = [];

    // --- 1. DEBT-TO-INCOME (DTI) CALCULATION ---
    // Formula: (Monthly Debt Obligations + Proposed Loan Payment) / Gross Monthly Income
    
    // Calculate Proposed Monthly Payment (PMT)
    const monthlyRate = (interestRate / 100) / 12;
    const proposedInstallment = (loanAmount * monthlyRate) / (1 - Math.pow(1 + monthlyRate, -loanTermMonths));
    
    const totalMonthlyObligations = financials.existingDebtService + proposedInstallment;
    
    // Avoid division by zero
    const income = Math.max(financials.monthlyIncome, 1);
    const dtiRatio = (totalMonthlyObligations / income) * 100;

    // Scoring Logic based on DTI
    if (dtiRatio > 50) {
        score -= 40;
        factors.push(`Critical: DTI Ratio is too high (${dtiRatio.toFixed(1)}%). Limit is 50%.`);
    } else if (dtiRatio > 40) {
        score -= 20;
        factors.push(`Warning: DTI Ratio is elevated (${dtiRatio.toFixed(1)}%).`);
    } else if (dtiRatio > 30) {
        score -= 10;
        factors.push(`Notice: DTI Ratio is moderate (${dtiRatio.toFixed(1)}%).`);
    } else {
        factors.push(`Positive: Healthy DTI Ratio (${dtiRatio.toFixed(1)}%).`);
    }

    // --- 2. DISPOSABLE INCOME CHECK ---
    const disposableIncome = financials.monthlyIncome - financials.monthlyExpense - financials.existingDebtService - proposedInstallment;
    if (disposableIncome < 0) {
        score -= 50; // Automatic fail essentially
        factors.push(`Critical: Negative Disposable Income (GHS ${disposableIncome.toFixed(2)}). Borrower cannot afford loan.`);
    } else if (disposableIncome < (financials.monthlyIncome * 0.1)) {
        score -= 15;
        factors.push(`Warning: Low Disposable Income buffer (<10%).`);
    }

    // --- 3. NET WORTH CHECK (Asset Coverage) ---
    const netWorth = financials.totalAssets - financials.totalLiabilities;
    if (netWorth < loanAmount) {
        score -= 10;
        factors.push(`Notice: Net worth is less than loan amount.`);
    } else {
        factors.push(`Positive: Strong asset coverage.`);
    }

    // --- 4. LOAN SIZE RISK ---
    if (loanAmount > (financials.monthlyIncome * 12)) {
        score -= 15;
        factors.push(`Risk: Loan amount exceeds 1 year of gross income.`);
    }

    // --- 5. KYC TIER ASSESSMENT ---
    if (kycLevel === 'Tier 1') {
        if (loanAmount > 5000) {
            score -= 30; // Heavy penalty for exceeding regulatory cap
            factors.push(`Critical: Loan GHS ${loanAmount.toLocaleString()} exceeds Tier 1 limit of 5,000.`);
        } else {
            score -= 5; // Slight penalty for low documentation
            factors.push(`Notice: Tier 1 account has limited documentation.`);
        }
    } else if (kycLevel === 'Tier 2') {
        if (loanAmount > 20000) {
            score -= 15;
            factors.push(`Warning: Loan exceeds Tier 2 recommended cap of 20,000.`);
        }
    } else if (kycLevel === 'Tier 3') {
        score += 10;
        factors.push(`Positive: Tier 3 Full KYC Verified.`);
    }

    // --- 6. LOAN AMOUNT TO TERM RATIO (Structure Risk) ---
    // Penalize loans where the term is disproportionately short for the amount,
    // creating a high "Principal Pressure" regardless of interest.
    const principalPerMonth = loanAmount / Math.max(loanTermMonths, 1);
    const amountToTermRatio = principalPerMonth; 

    // Heuristic: If repaying > 20% of the principal in a single month (term < 5 months) for large loans.
    // Or if the principal portion takes up a huge chunk of income.
    
    // Check 1: Structure Check (Independent of Income)
    // If loan is significant (> 5000) and term is very short (< 4 months)
    if (loanAmount > 5000 && loanTermMonths < 4) {
        score -= 10;
        factors.push(`Risk: Aggressive repayment structure. Term (${loanTermMonths}m) is short for amount.`);
    }

    // Check 2: Affordability of Principal (Dependent on Income)
    const principalBurden = (principalPerMonth / income) * 100;
    if (principalBurden > 35) {
        score -= 15;
        factors.push(`Risk: Principal repayment alone consumes ${principalBurden.toFixed(1)}% of monthly income.`);
    }

    // --- FINAL GRADING ---
    // Ensure score stays within 0-100
    score = Math.max(0, Math.min(100, score));

    let grade: CreditScore['grade'] = 'F';
    let recommendation: CreditScore['recommendation'] = 'REJECT';
    let maxLimit = 0;

    if (score >= 80) {
        grade = 'A';
        recommendation = 'APPROVE';
        maxLimit = loanAmount * 1.5; // Eligible for more
    } else if (score >= 70) {
        grade = 'B';
        recommendation = 'APPROVE';
        maxLimit = loanAmount;
    } else if (score >= 50) {
        grade = 'C';
        recommendation = 'REVIEW';
        maxLimit = loanAmount * 0.8; // Reduce exposure
    } else if (score >= 30) {
        grade = 'D';
        recommendation = 'REVIEW';
        maxLimit = loanAmount * 0.5;
    } else {
        grade = 'F';
        recommendation = 'REJECT';
        maxLimit = 0;
    }

    return {
        score: Math.round(score),
        grade,
        recommendation,
        maxLoanLimit: Math.round(maxLimit),
        factors,
        calculatedAt: new Date().toISOString()
    };
};
