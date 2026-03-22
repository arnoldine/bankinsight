import React from 'react';
import JsonFormRenderer, { JsonSchemaForm } from './JsonFormRenderer';
import { FlaskConical, Settings2 } from 'lucide-react';

const sampleSchema: JsonSchemaForm = {
    title: "Susu Operations Configuration",
    description: "Define localized parameters for daily micro-deposit collection logic.",
    type: "object",
    required: ["collectionRegion", "dailyMinimum", "allowOvernightHold"],
    properties: {
        "collectionRegion": {
            type: "string",
            title: "Operating Region",
            enum: ["Greater Accra", "Ashanti", "Northern", "Volta"]
        },
        "dailyMinimum": {
            type: "number",
            title: "Minimum Daily Collection (GHS)",
            minimum: 5.0,
            maximum: 1000.0,
            description: "The minimum limit a Susu collector must gather per client before it's recognized."
        },
        "collectorIdPrefix": {
            type: "string",
            title: "Device Prefix ID",
            minLength: 3,
            maxLength: 5,
            pattern: "^[A-Z]{3,5}$",
            description: "Must be 3-5 uppercase letters (e.g. ACC, KUM)"
        },
        "allowOvernightHold": {
            type: "boolean",
            title: "Allow Overnight Cash Holds",
            description: "If checked, collectors are not required to hit the Branch Vault EOD reconciliation strictly."
        }
    }
};

export default function ExtensibilityTestPage() {
    return (
        <div className="p-6 space-y-6">
            <div className="mb-6">
                <h1 className="text-2xl font-bold flex items-center gap-2">
                    <Settings2 className="w-6 h-6 text-blue-400" />
                    Tier 2/3 Extensibility Sandbox
                </h1>
                <p className="text-gray-500 dark:text-slate-400 mt-1">
                    Sandbox only. This page tests the dynamic <code>JsonFormRenderer</code> against a sample draft-07 schema and does not persist configuration to the backend.
                </p>
            </div>

            <div className="rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900 flex items-start gap-3">
                <FlaskConical className="mt-0.5 h-4 w-4 flex-none" />
                <span>This is a developer-facing sandbox for schema rendering and payload inspection, not a live extensibility registry.</span>
            </div>

            <JsonFormRenderer
                schema={sampleSchema}
                onSubmit={(data) => {
                    console.info('Extensibility sandbox submission captured locally.', data);
                }}
            />
        </div>
    );
}
