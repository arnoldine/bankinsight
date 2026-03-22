import React, { useState } from 'react';
import { AlertCircle, CheckCircle2 } from 'lucide-react';

/**
 * Simplified JSON Schema interface matching standard draft-07 shapes
 * used for generating dynamic metadata banking forms.
 */
export interface JsonSchemaForm {
    title: string;
    description?: string;
    type: 'object';
    required?: string[];
    properties: {
        [key: string]: {
            type: 'string' | 'number' | 'integer' | 'boolean';
            title?: string;
            description?: string;
            enum?: string[] | number[];
            minimum?: number;
            maximum?: number;
            minLength?: number;
            maxLength?: number;
            pattern?: string;
        }
    }
}

interface JsonFormRendererProps {
    schema: JsonSchemaForm;
    initialData?: any;
    onSubmit: (formData: any) => void;
    isLoading?: boolean;
}

export default function JsonFormRenderer({
    schema,
    initialData = {},
    onSubmit,
    isLoading = false
}: JsonFormRendererProps) {

    const [formData, setFormData] = useState<any>(initialData);
    const [errors, setErrors] = useState<Record<string, string>>({});
    const [isSubmitted, setIsSubmitted] = useState(false);

    // Handle generic input changes
    const handleChange = (fieldId: string, value: any, propertyDef: any) => {
        // Type coercion if necessary
        let parsedValue = value;
        if (propertyDef.type === 'number' || propertyDef.type === 'integer') {
            parsedValue = value === '' ? '' : Number(value);
        }

        setFormData((prev: any) => ({
            ...prev,
            [fieldId]: parsedValue
        }));

        // Clear specific field error when they start typing
        if (errors[fieldId]) {
            setErrors(prev => {
                const newErrs = { ...prev };
                delete newErrs[fieldId];
                return newErrs;
            });
        }
        setIsSubmitted(false);
    };

    // Basic Client-Side Validation against Schema Rules
    const validateForm = (): boolean => {
        const newErrors: Record<string, string> = {};

        // 1. Check Required Fields
        (schema.required || []).forEach(reqField => {
            if (formData[reqField] === undefined || formData[reqField] === null || formData[reqField] === '') {
                newErrors[reqField] = 'This field is required';
            }
        });

        // 2. Attribute validation (Min, Max, Regex)
        Object.entries(schema.properties).forEach(([key, prop]) => {
            const val = formData[key];
            if (val === undefined || val === null || val === '') return; // Skip if empty (handled by required check if necessary)

            if (prop.type === 'string') {
                const strVal = String(val);
                if (prop.minLength && strVal.length < prop.minLength) {
                    newErrors[key] = `Minimum length is ${prop.minLength}`;
                }
                if (prop.maxLength && strVal.length > prop.maxLength) {
                    newErrors[key] = `Maximum length is ${prop.maxLength}`;
                }
                if (prop.pattern && !new RegExp(prop.pattern).test(strVal)) {
                    newErrors[key] = `Format is invalid`;
                }
            }

            if (prop.type === 'number' || prop.type === 'integer') {
                const numVal = Number(val);
                if (prop.minimum !== undefined && numVal < prop.minimum) {
                    newErrors[key] = `Minimum value is ${prop.minimum}`;
                }
                if (prop.maximum !== undefined && numVal > prop.maximum) {
                    newErrors[key] = `Maximum value is ${prop.maximum}`;
                }
            }
        });

        setErrors(newErrors);
        return Object.keys(newErrors).length === 0;
    };

    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        if (validateForm()) {
            onSubmit(formData);
            setIsSubmitted(true);
        }
    };

    // Field Renderer
    const renderField = (fieldId: string, property: any) => {
        const isRequired = schema.required?.includes(fieldId);
        const hasError = !!errors[fieldId];
        const value = formData[fieldId] !== undefined ? formData[fieldId] : '';

        return (
            <div key={fieldId} className="mb-4">
                <label className="block text-sm font-medium text-gray-600 dark:text-slate-300 mb-1">
                    {property.title || fieldId} {isRequired && <span className="text-red-400">*</span>}
                </label>

                {property.description && (
                    <p className="text-xs text-gray-400 dark:text-slate-500 mb-2">{property.description}</p>
                )}

                {/* ENUM RENDERER -> SELECT */}
                {property.enum ? (
                    <select
                        value={value}
                        onChange={(e) => handleChange(fieldId, e.target.value, property)}
                        className={`w-full bg-gray-100 dark:bg-slate-900 border rounded-lg px-4 py-2 text-gray-900 dark:text-white 
              ${hasError ? 'border-red-500' : 'border-gray-200 dark:border-slate-700 focus:border-blue-500'}`}
                    >
                        <option value="">-- Select an option --</option>
                        {property.enum.map((opt: any) => (
                            <option key={opt} value={opt}>{opt}</option>
                        ))}
                    </select>
                )

                    /* BOOLEAN RENDERER -> CHECKBOX */
                    : property.type === 'boolean' ? (
                        <div className="flex items-center mt-2">
                            <input
                                type="checkbox"
                                checked={!!value}
                                onChange={(e) => handleChange(fieldId, e.target.checked, property)}
                                className="w-4 h-4 bg-gray-100 dark:bg-slate-900 border-gray-200 dark:border-slate-700 rounded text-blue-600 focus:ring-blue-500 focus:ring-2"
                            />
                            <span className="ml-2 text-sm text-gray-600 dark:text-slate-300">Enable</span>
                        </div>
                    )

                        /* NUMBER RENDERER -> NUMBER INPUT */
                        : property.type === 'number' || property.type === 'integer' ? (
                            <input
                                type="number"
                                step={property.type === 'integer' ? '1' : '0.01'}
                                value={value}
                                onChange={(e) => handleChange(fieldId, e.target.value, property)}
                                className={`w-full bg-gray-100 dark:bg-slate-900 border rounded-lg px-4 py-2 text-gray-900 dark:text-white 
              ${hasError ? 'border-red-500' : 'border-gray-200 dark:border-slate-700 focus:border-blue-500'}`}
                            />
                        )

                            /* DEFAULT (STRING) RENDERER -> TEXT INPUT */
                            : (
                                <input
                                    type="text"
                                    value={value}
                                    onChange={(e) => handleChange(fieldId, e.target.value, property)}
                                    className={`w-full bg-gray-100 dark:bg-slate-900 border rounded-lg px-4 py-2 text-gray-900 dark:text-white 
              ${hasError ? 'border-red-500' : 'border-gray-200 dark:border-slate-700 focus:border-blue-500'}`}
                                />
                            )}

                {/* Error Label */}
                {hasError && (
                    <p className="text-red-400 text-xs mt-1">{errors[fieldId]}</p>
                )}
            </div>
        );
    };

    return (
        <div className="bg-white dark:bg-slate-800 border border-gray-200 dark:border-slate-700 rounded-lg p-6 max-w-2xl shadow-xl">
            <div className="mb-6 pb-4 border-b border-gray-200 dark:border-slate-700">
                <h2 className="text-2xl font-bold text-gray-900 dark:text-white">{schema.title}</h2>
                {schema.description && (
                    <p className="text-gray-500 dark:text-slate-400 mt-1">{schema.description}</p>
                )}
            </div>

            <form onSubmit={handleSubmit} noValidate>

                {/* Iterate over all properties dynamically mapped in schema */}
                <div className="space-y-2">
                    {Object.entries(schema.properties).map(([key, prop]) =>
                        renderField(key, prop)
                    )}
                </div>

                {/* Final Submission Block */}
                <div className="mt-8 pt-4 border-t border-gray-200 dark:border-slate-700 flex items-center justify-between">
                    <button
                        type="submit"
                        disabled={isLoading}
                        className="px-6 py-2 bg-blue-600 hover:bg-blue-700 text-white font-medium rounded-lg transition-colors disabled:opacity-50"
                    >
                        {isLoading ? 'Processing...' : 'Submit Form'}
                    </button>

                    {isSubmitted && !isLoading && Object.keys(errors).length === 0 && (
                        <span className="flex items-center text-green-400 text-sm gap-1">
                            <CheckCircle2 className="w-4 h-4" /> Passed Validation
                        </span>
                    )}

                    {Object.keys(errors).length > 0 && (
                        <span className="flex items-center text-red-400 text-sm gap-1">
                            <AlertCircle className="w-4 h-4" /> Please fix validation errors
                        </span>
                    )}
                </div>
            </form>
        </div>
    );
}
