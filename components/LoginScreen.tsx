import React, { useState } from 'react';
import { Lock, User, ShieldCheck } from 'lucide-react';

interface LoginScreenProps {
    onLogin: (id: string, pass: string) => void;
    onOAuthLogin: (provider: string) => void;
    error: string | null;
}

const LoginScreen: React.FC<LoginScreenProps> = ({ onLogin, onOAuthLogin, error }) => {
    const [userId, setUserId] = useState('admin@bankinsight.local');
    const [password, setPassword] = useState('password123');

    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        onLogin(userId, password);
    };

    return (
        <div className="min-h-screen flex items-center justify-center bg-drk-900 relative overflow-hidden font-sans">
            {/* Dynamic Background Gradient Blobs */}
            <div className="absolute top-0 left-0 w-full h-full opacity-30 pointer-events-none">
                <div className="absolute top-[-10%] left-[-10%] w-[40%] h-[40%] bg-brand-600 rounded-full mix-blend-screen filter blur-[100px] animate-pulse"></div>
                <div className="absolute bottom-[-10%] right-[-10%] w-[50%] h-[50%] bg-blue-600 rounded-full mix-blend-screen filter blur-[120px] animate-pulse" style={{ animationDelay: '2s' }}></div>
                <div className="absolute top-[20%] right-[20%] w-[30%] h-[30%] bg-purple-600 rounded-full mix-blend-screen filter blur-[90px] animate-pulse" style={{ animationDelay: '4s' }}></div>
            </div>

            <div className="w-full max-w-md p-8 relative z-10 bg-drk-800/60 backdrop-blur-xl border border-white/10 rounded-3xl shadow-[0_8px_32px_0_rgba(0,0,0,0.36)]">
                <div className="text-center mb-10">
                    <div className="bg-gradient-to-br from-brand-500 to-blue-600 w-16 h-16 rounded-2xl flex items-center justify-center mx-auto mb-5 shadow-[0_0_20px_rgba(20,184,166,0.4)]">
                        <ShieldCheck className="text-gray-900 dark:text-white" size={32} strokeWidth={1.5} />
                    </div>
                    <h1 className="text-3xl font-heading font-bold text-gray-900 dark:text-white tracking-tight">BankInsight</h1>
                    <p className="text-brand-100/60 text-sm mt-2 font-medium">Core Banking Engine v10.2</p>
                </div>

                <form onSubmit={handleSubmit} className="space-y-5">
                    <div>
                        <label className="block text-[11px] font-bold text-white/50 uppercase tracking-wider mb-2">Staff ID / Email</label>
                        <div className="relative group">
                            <User className="absolute left-4 top-1/2 -translate-y-1/2 text-white/40 group-focus-within:text-brand-500 transition-colors" size={18} />
                            <input
                                type="text"
                                className="w-full pl-11 pr-4 py-3.5 bg-drk-900/50 border border-white/10 text-white rounded-xl focus:outline-none focus:ring-2 focus:ring-brand-500/50 focus:border-brand-500 transition-all placeholder:text-white/20"
                                placeholder="admin@bankinsight.local"
                                value={userId}
                                onChange={e => setUserId(e.target.value)}
                            />
                        </div>
                    </div>

                    <div>
                        <div className="flex justify-between items-center mb-2">
                            <label className="block text-[11px] font-bold text-white/50 uppercase tracking-wider">Password</label>
                            <a href="#" className="text-[11px] text-brand-500 hover:text-brand-400 font-medium transition-colors">Forgot?</a>
                        </div>
                        <div className="relative group">
                            <Lock className="absolute left-4 top-1/2 -translate-y-1/2 text-white/40 group-focus-within:text-brand-500 transition-colors" size={18} />
                            <input
                                type="password"
                                className="w-full pl-11 pr-4 py-3.5 bg-drk-900/50 border border-white/10 text-white rounded-xl focus:outline-none focus:ring-2 focus:ring-brand-500/50 focus:border-brand-500 transition-all placeholder:text-white/20"
                                placeholder="••••••••"
                                value={password}
                                onChange={e => setPassword(e.target.value)}
                            />
                        </div>
                    </div>

                    {error && (
                        <div className="p-3 bg-red-500/10 border border-red-500/20 text-red-400 rounded-xl text-sm font-medium text-center backdrop-blur-sm">
                            {error}
                        </div>
                    )}

                    <button
                        type="submit"
                        className="w-full bg-gradient-to-r from-brand-600 to-brand-500 hover:from-brand-500 hover:to-brand-400 text-white font-semibold py-3.5 flex justify-center items-center gap-2 rounded-xl shadow-[0_0_20px_rgba(20,184,166,0.3)] hover:shadow-[0_0_25px_rgba(20,184,166,0.5)] transition-all transform hover:-translate-y-0.5 active:translate-y-0"
                    >
                        Secure Login <Lock size={16} className="opacity-70" />
                    </button>
                </form>

                <div className="my-8 flex items-center justify-between opacity-50">
                    <span className="w-1/4 border-b border-white/20"></span>
                    <div className="text-[10px] text-center text-gray-900 dark:text-white uppercase tracking-widest font-semibold">Or continue with</div>
                    <span className="w-1/4 border-b border-white/20"></span>
                </div>

                <div className="space-y-3">
                    <button
                        onClick={() => onOAuthLogin('AZURE_AD')}
                        className="w-full flex items-center justify-center gap-3 py-3 border border-white/10 bg-white/5 hover:bg-white/10 rounded-xl text-sm font-medium text-gray-900 dark:text-white transition-all hover:border-white/20"
                    >
                        <svg className="w-4 h-4" viewBox="0 0 21 21">
                            <path fill="#f25022" d="M1 1h9v9H1z" /><path fill="#00a4ef" d="M1 11h9v9H1z" /><path fill="#7fba00" d="M11 1h9v9H11z" /><path fill="#ffb900" d="M11 11h9v9H11z" />
                        </svg>
                        Microsoft 365
                    </button>
                    <button
                        onClick={() => onOAuthLogin('LDAP')}
                        className="w-full flex items-center justify-center gap-3 py-3 border border-white/10 bg-white/5 hover:bg-white/10 rounded-xl text-sm font-medium text-gray-900 dark:text-white transition-all hover:border-white/20"
                    >
                        <svg className="w-4 h-4 text-brand-400" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10" />
                        </svg>
                        Corporate Directory
                    </button>
                </div>

                <div className="mt-8 pt-6 border-t border-white/10 text-center text-[10px] text-white/40 uppercase tracking-wider">
                    <p>Protected by BoG Cyber Security Directive 2023</p>
                </div>
            </div>
        </div>
    );
};

export default LoginScreen;