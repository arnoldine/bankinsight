import { GoogleGenAI, Type } from "@google/genai";
import { Account, AIInsight, Customer, Loan, MigrationFieldMapping } from '../types';

const getClient = () => new GoogleGenAI({ apiKey: process.env.API_KEY });

export const generateDashboardInsights = async (data: any): Promise<AIInsight[]> => {
  try {
    const ai = getClient();
    // Summarize critical data points
    const context = {
       liquidity: "45%", // Mock
       unmatchedFX: "120000 USD",
       nplRatio: "8.5%",
       totalDeposits: 45000000
    };

    const response = await ai.models.generateContent({
      model: 'gemini-3-flash-preview',
      contents: `You are the Core Banking System Architect for a Bank of Ghana licensed institution.
      Analyze the following Bank Key Risk Indicators (KRIs):
      ${JSON.stringify(context)}
      
      Identify 3 critical insights related to:
      1. Liquidity compliance (BoG requirement > 35%).
      2. FX Exposure limits.
      3. Credit Risk (NPL).
      
      Return purely in JSON format.`,
      config: {
        responseMimeType: "application/json",
        responseSchema: {
          type: Type.ARRAY,
          items: {
            type: Type.OBJECT,
            properties: {
              title: { type: Type.STRING },
              description: { type: Type.STRING },
              severity: { type: Type.STRING, enum: ['low', 'medium', 'high'] },
              type: { type: Type.STRING, enum: ['compliance', 'fraud', 'liquidity'] }
            },
            required: ['title', 'description', 'severity', 'type']
          }
        }
      }
    });

    if (response.text) {
        return JSON.parse(response.text) as AIInsight[];
    }
    return [];
  } catch (error) {
    console.error("Error generating banking insights:", error);
    return [];
  }
};

export const chatWithPostgreSQL = async (history: string[], currentMessage: string, contextData: any): Promise<string> => {
    try {
        const ai = getClient();
        
        const prompt = `
        System: You are an Expert PostgreSQL Developer specializing in Core Banking Systems (T24, Flexcube style logic but built on Linear Hash).
        
        You understand:
        - BoG (Bank of Ghana) Prudential Returns.
        - Chart of Accounts (General Ledger).
        - PL/pgSQL Stored Procedures.
        - Linear Hash Dictionaries (Dict.MFS).
        
        Chat History:
        ${history.join('\n')}
        
        User: ${currentMessage}
        
        Answer professionally as a Senior Systems Analyst.
        `;

        const response = await ai.models.generateContent({
            model: 'gemini-3-flash-preview',
            contents: prompt,
        });

        return response.text || "System Busy.";
    } catch (error) {
        return "Error connecting to Core Banking AI.";
    }
};

export const runBasicPlusCode = async (code: string, contextData: any): Promise<string> => {
    try {
        const ai = getClient();
        
        const prompt = `
        System: You are the PostgreSQL 10.2 Engine.
        Execute the following PL/pgSQL Code.
        
        Context:
        - The code is likely calculating Loan Interest (Reducing Balance) or formatting JSON for O4W.
        - Return the printed output or the return value.
        - If the code uses 'Calculations', perform them accurately.
        
        Code:
        ${code}
        `;
        
        const response = await ai.models.generateContent({
            model: 'gemini-3-flash-preview',
            contents: prompt,
        });

        return response.text || "No output.";
    } catch (error) {
        return "FS109: SYS_NET_ERR";
    }
};

export const autoMapMigrationFields = async (legacyHeaders: string[], targetDictionaries: string[]): Promise<MigrationFieldMapping[]> => {
    try {
        const ai = getClient();
        
        const prompt = `
        System: You are a Data Migration Specialist moving legacy banking data to PostgreSQL.
        
        Task: Map the 'Legacy Headers' to the best matching 'Target Dictionary' field.
        
        Legacy Headers: ${JSON.stringify(legacyHeaders)}
        Target Dictionaries: ${JSON.stringify(targetDictionaries)}
        
        Return JSON mapping.
        `;
        
        const response = await ai.models.generateContent({
            model: 'gemini-3-flash-preview',
            contents: prompt,
            config: {
                responseMimeType: "application/json",
                responseSchema: {
                    type: Type.ARRAY,
                    items: {
                        type: Type.OBJECT,
                        properties: {
                            legacyHeader: { type: Type.STRING },
                            targetDict: { type: Type.STRING },
                            confidence: { type: Type.NUMBER, description: "0 to 1 confidence" }
                        },
                        required: ['legacyHeader', 'targetDict', 'confidence']
                    }
                }
            }
        });

        if (response.text) {
            return JSON.parse(response.text) as MigrationFieldMapping[];
        }
        return [];
    } catch (error) {
        console.error("Mapping error", error);
        // Fallback default mapping
        return legacyHeaders.map(h => ({ legacyHeader: h, targetDict: '', confidence: 0 }));
    }
};

export const generateMigrationBasicPlusCode = async (mappings: MigrationFieldMapping[], targetTable: string = "DATA_VOL"): Promise<string> => {
    try {
        const ai = getClient();
        
        const validMappings = mappings.filter(m => m.targetDict && m.targetDict !== '');
        
        const prompt = `
        System: You are an expert PostgreSQL PL/pgSQL Programmer.
        
        Task: Generate a robust PL/pgSQL Stored Procedure to import CSV data into the '${targetTable}' table.
        
        Mappings provided (Legacy Header -> OI Dictionary):
        ${JSON.stringify(validMappings)}
        
        Code Requirements:
        1. Function Name: IMPORT_BATCH_${new Date().getTime().toString().slice(-6)}
        2. Open the table '${targetTable}'.
        3. Use OSREAD to load 'C:\\TEMP\\IMPORT_DATA.CSV'.
        4. Loop through lines (Swap CHAR(13):CHAR(10) with @FM).
        5. For each line:
           - Parse CSV columns (handle quotes if possible, or simple Swap).
           - Assign values to specific Field Marks (FM) based on the mappings.
             (Assign plausible Field Numbers, e.g., <1>, <2> etc for the dictionaries provided in the mapping).
           - Use 'Write Rec on FileVar, ID'
        6. Add comments explaining the mapping.
        7. Return a status string "Imported X records".
        8. Include error handling for file not found.
        
        Output: Return ONLY the PL/pgSQL code block.
        `;
        
        const response = await ai.models.generateContent({
            model: 'gemini-3-flash-preview',
            contents: prompt,
        });

        return response.text || "REM Code generation failed.";
    } catch (error) {
        console.error("Code generation error", error);
        return "REM Error connecting to AI service.";
    }
};

export const generateBasicPlusCode = async (description: string, template: string): Promise<string> => {
    try {
        const ai = getClient();
        
        const prompt = `
        System: You are an expert PostgreSQL PL/pgSQL (RevG) Developer.
        
        Task: Generate a robust, compilable PL/pgSQL module.
        
        Template Context: ${template}
        User Requirements: ${description}
        
        Technical Constraints:
        - For 'O4W API Endpoint': Assume incoming JSON string in 'Request', return JSON string. Use 'O4WJSON' parser if needed or simple string manipulation.
        - For 'Validation': Return 1 (Valid) or 0 (Invalid) and set 'ANS' or specific error variable.
        - For 'Dictionary MFS': Handle standard MFS codes (READ, WRITE, DELETE).
        - Syntax: Must be valid PL/pgSQL. Use 'Compile Function' or 'Compile Subroutine'.
        - Include comments explaining the logic.
        
        Output: RAW CODE ONLY. Do not use markdown backticks.
        `;
        
        const response = await ai.models.generateContent({
            model: 'gemini-3-flash-preview',
            contents: prompt,
        });

        let code = response.text || "REM Generation Failed";
        // Cleanup markdown
        code = code.replace(/```basic\+?/gi, '').replace(/```/g, '').trim();
        return code;
    } catch (error) {
        console.error("AI Gen Error", error);
        return "REM Error connecting to AI Service";
    }
};

export const runSQLCode = async (code: string, contextData: any): Promise<string> => {
    try {
        const ai = getClient();
        
        const prompt = `
        System: You are a PostgreSQL Database Engine.
        Execute the following SQL code and return the result.
        
        Context Data: ${JSON.stringify(contextData)}
        
        Code:
        ${code}
        
        Return the query results or execution status.
        `;
        
        const response = await ai.models.generateContent({
            model: 'gemini-3-flash-preview',
            contents: prompt,
        });

        return response.text || "No output.";
    } catch (error) {
        return "Error executing SQL code.";
    }
};

export const generateSQLCode = async (description: string, template?: string): Promise<string> => {
    try {
        const ai = getClient();
        
        const prompt = `
        System: You are an expert PostgreSQL developer.
        
        Task: Generate PostgreSQL SQL code based on the following description:
        ${description}
        
        ${template ? `Template/Context: ${template}` : ''}
        
        Requirements:
        - Return valid PostgreSQL syntax
        - Include comments explaining the code
        - Output: RAW SQL CODE ONLY. Do not use markdown backticks.
        `;
        
        const response = await ai.models.generateContent({
            model: 'gemini-3-flash-preview',
            contents: prompt,
        });

        let code = response.text || "-- Generation Failed";
        // Cleanup markdown
        code = code.replace(/```sql/gi, '').replace(/```/g, '').trim();
        return code;
    } catch (error) {
        console.error("SQL Gen Error", error);
        return "-- Error connecting to AI Service";
    }
};