import React, { useState, useRef, useEffect } from 'react';
import { Send, Bot, User, Sparkles } from 'lucide-react';
import { ChatMessage, Account } from '../types';
import { chatWithPostgreSQL } from '../services/geminiService';

interface ChatInterfaceProps {
    data: Account[];
}

const ChatInterface: React.FC<ChatInterfaceProps> = ({ data }) => {
    const [messages, setMessages] = useState<ChatMessage[]>([
        { id: '1', role: 'ai', content: 'Welcome to PostgreSQL O4W. I can help you generate RList queries, write PL/pgSQL code, or analyze your Linear Hash data.', timestamp: new Date() }
    ]);
    const [input, setInput] = useState('');
    const [isTyping, setIsTyping] = useState(false);
    const messagesEndRef = useRef<HTMLDivElement>(null);

    const scrollToBottom = () => {
        messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
    };

    useEffect(() => {
        scrollToBottom();
    }, [messages]);

    const handleSend = async () => {
        if (!input.trim() || isTyping) return;

        const userMsg: ChatMessage = {
            id: Date.now().toString(),
            role: 'user',
            content: input,
            timestamp: new Date()
        };

        setMessages(prev => [...prev, userMsg]);
        setInput('');
        setIsTyping(true);

        // Prepare history for context
        const historyStrings = messages.map(m => `${m.role.toUpperCase()}: ${m.content}`);

        const responseText = await chatWithPostgreSQL(historyStrings, userMsg.content, data);

        const aiMsg: ChatMessage = {
            id: (Date.now() + 1).toString(),
            role: 'ai',
            content: responseText,
            timestamp: new Date()
        };

        setMessages(prev => [...prev, aiMsg]);
        setIsTyping(false);
    };

    return (
        <div className="bg-white rounded-xl shadow-sm border border-gray-100 flex flex-col h-full">
            <div className="p-4 border-b border-gray-100 bg-gradient-to-r from-blue-800 to-blue-900 rounded-t-xl text-white">
                <h2 className="font-semibold flex items-center gap-2">
                    <Sparkles size={18} className="text-yellow-400" />
                    O4W AI Assistant
                </h2>
                <p className="text-blue-200 text-xs mt-1">PostgreSQL 10.2 / Gemini 3</p>
            </div>

            <div className="flex-1 overflow-y-auto p-4 space-y-4">
                {messages.map((msg) => (
                    <div key={msg.id} className={`flex ${msg.role === 'user' ? 'justify-end' : 'justify-start'}`}>
                        <div className={`flex gap-3 max-w-[85%] ${msg.role === 'user' ? 'flex-row-reverse' : 'flex-row'}`}>
                            <div className={`w-8 h-8 rounded-full flex items-center justify-center flex-shrink-0 ${
                                msg.role === 'user' ? 'bg-gray-200' : 'bg-blue-100 text-blue-800'
                            }`}>
                                {msg.role === 'user' ? <User size={16} /> : <Bot size={16} />}
                            </div>
                            <div className={`p-3 rounded-2xl text-sm ${
                                msg.role === 'user' 
                                ? 'bg-gray-900 text-white rounded-tr-none' 
                                : 'bg-gray-100 text-gray-800 rounded-tl-none'
                            }`}>
                                {msg.content}
                            </div>
                        </div>
                    </div>
                ))}
                {isTyping && (
                    <div className="flex justify-start">
                        <div className="flex gap-3 max-w-[85%]">
                             <div className="w-8 h-8 rounded-full bg-blue-100 text-blue-800 flex items-center justify-center">
                                <Bot size={16} />
                            </div>
                            <div className="bg-gray-100 p-3 rounded-2xl rounded-tl-none text-gray-500 text-sm italic">
                                Processing RList request...
                            </div>
                        </div>
                    </div>
                )}
                <div ref={messagesEndRef} />
            </div>

            <div className="p-3 border-t border-gray-100">
                <div className="relative flex items-center">
                    <input
                        type="text"
                        value={input}
                        onChange={(e) => setInput(e.target.value)}
                        onKeyPress={(e) => e.key === 'Enter' && handleSend()}
                        placeholder="Ask about records, or type an RList command..."
                        className="w-full pl-4 pr-12 py-3 bg-gray-50 border border-gray-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm"
                    />
                    <button 
                        onClick={handleSend}
                        disabled={!input.trim() || isTyping}
                        className="absolute right-2 p-2 bg-blue-800 text-white rounded-lg hover:bg-blue-900 disabled:opacity-50 transition-colors"
                    >
                        <Send size={16} />
                    </button>
                </div>
            </div>
        </div>
    );
};

export default ChatInterface;