
import React, { useState } from 'react';
import { StaffUser } from '../types';
import { User, Mail, Phone, MapPin, BadgeCheck, Camera, Save, X, Lock, Bell, Shield, Key } from 'lucide-react';

interface UserProfileProps {
  user: StaffUser;
  onUpdate: (updatedUser: StaffUser) => void;
  onChangePassword: (newPass: string) => void;
}

const UserProfile: React.FC<UserProfileProps> = ({ user, onUpdate, onChangePassword }) => {
  const [isEditing, setIsEditing] = useState(false);
  const [formData, setFormData] = useState<StaffUser>(user);
  const [notification, setNotification] = useState<{ tone: 'success' | 'error'; message: string } | null>(null);
  
  // Password Change State
  const [showPasswordModal, setShowPasswordModal] = useState(false);
  const [passwordForm, setPasswordForm] = useState({ new: '', confirm: '' });

  const handleInputChange = (field: keyof StaffUser, value: string) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  const handleSave = () => {
    // Basic validation
    if (!formData.name || !formData.email) {
      setNotification({ tone: 'error', message: 'Name and email are required.' });
      setTimeout(() => setNotification(null), 3000);
      return;
    }

    onUpdate(formData);
    setIsEditing(false);
    setNotification({ tone: 'success', message: 'Profile updated successfully.' });
    setTimeout(() => setNotification(null), 3000);
  };

  const handleCancel = () => {
    setFormData(user);
    setIsEditing(false);
  };

  const handleChangePasswordSubmit = () => {
      if (passwordForm.new !== passwordForm.confirm) {
          setNotification({ tone: 'error', message: 'Passwords do not match.' });
          return;
      }
      if (passwordForm.new.length < 6) {
          setNotification({ tone: 'error', message: 'Password must be at least 6 characters long.' });
          return;
      }
      onChangePassword(passwordForm.new);
      setShowPasswordModal(false);
      setPasswordForm({ new: '', confirm: '' });
      setNotification({ tone: 'success', message: 'Password changed successfully.' });
      setTimeout(() => setNotification(null), 3000);
  };

  return (
    <div className="max-w-5xl mx-auto h-full overflow-y-auto p-2 relative">
      <div className="mb-6">
         <h1 className="text-2xl font-bold text-gray-900">My Profile</h1>
         <p className="text-gray-500 text-sm">Manage your account settings and preferences.</p>
      </div>

      {notification && (
        <div className={`mb-4 flex items-center gap-2 rounded-lg border p-4 animate-in fade-in slide-in-from-top-2 ${
          notification.tone === 'success'
            ? 'border-green-200 bg-green-50 text-green-700'
            : 'border-rose-200 bg-rose-50 text-rose-700'
        }`}>
           <BadgeCheck size={18} /> {notification.message}
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        
        {/* Left Column: ID Card */}
        <div className="lg:col-span-1 space-y-6">
           <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden relative">
              <div className="h-24 bg-gradient-to-r from-blue-600 to-blue-800"></div>
              <div className="px-6 pb-6">
                 <div className="relative -mt-12 mb-4 flex justify-center">
                    <div className="w-24 h-24 bg-white rounded-full p-1 shadow-md">
                        <div className="w-full h-full bg-blue-100 rounded-full flex items-center justify-center text-blue-700 text-3xl font-bold border-4 border-white">
                            {formData.avatarInitials}
                        </div>
                    </div>
                    {isEditing && (
                        <button className="absolute bottom-0 right-1/2 translate-x-10 bg-gray-800 text-white p-1.5 rounded-full hover:bg-gray-700 border-2 border-white">
                            <Camera size={14} />
                        </button>
                    )}
                 </div>
                 
                 <div className="text-center mb-6">
                    <h2 className="text-xl font-bold text-gray-900">{user.name}</h2>
                    <p className="text-sm text-gray-500">{user.roleName || 'Staff Member'} • {user.branchId}</p>
                    <div className="mt-2 flex justify-center">
                        <span className={`px-2 py-0.5 rounded-full text-xs font-medium border ${
                            user.status === 'Active' ? 'bg-green-50 text-green-700 border-green-200' : 'bg-gray-100 text-gray-600'
                        }`}>
                            {user.status}
                        </span>
                    </div>
                 </div>

                 <div className="border-t border-gray-100 pt-4 space-y-3">
                    <div className="flex items-center justify-between text-sm">
                        <span className="text-gray-500">Employee ID</span>
                        <span className="font-mono font-medium text-gray-700">{user.id}</span>
                    </div>
                    <div className="flex items-center justify-between text-sm">
                        <span className="text-gray-500">Last Login</span>
                        <span className="text-gray-700">{user.lastLogin}</span>
                    </div>
                 </div>
              </div>
           </div>

           <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
              <h3 className="font-bold text-gray-800 mb-4 flex items-center gap-2">
                  <Shield size={18} className="text-blue-600"/> Security
              </h3>
              <div className="space-y-4">
                  <button 
                    onClick={() => setShowPasswordModal(true)}
                    className="w-full py-2 px-4 border border-gray-300 rounded-lg text-sm text-gray-700 font-medium hover:bg-gray-50 flex items-center justify-center gap-2"
                  >
                      <Lock size={16} /> Change Password
                  </button>
                  <button className="w-full py-2 px-4 border border-gray-300 rounded-lg text-sm text-gray-700 font-medium hover:bg-gray-50 flex items-center justify-center gap-2">
                      <Bell size={16} /> Notification Settings
                  </button>
              </div>
           </div>
        </div>

        {/* Right Column: Details Form */}
        <div className="lg:col-span-2">
           <div className="bg-white rounded-xl shadow-sm border border-gray-200">
              <div className="p-6 border-b border-gray-200 flex justify-between items-center">
                  <h3 className="text-lg font-bold text-gray-800">Personal Information</h3>
                  {!isEditing ? (
                      <button 
                        onClick={() => setIsEditing(true)}
                        className="text-blue-600 hover:text-blue-800 text-sm font-medium"
                      >
                          Edit Profile
                      </button>
                  ) : (
                      <div className="flex gap-2">
                          <button 
                            onClick={handleCancel}
                            className="text-gray-500 hover:text-gray-700 p-1.5 rounded hover:bg-gray-100"
                          >
                              <X size={20} />
                          </button>
                          <button 
                            onClick={handleSave}
                            className="text-green-600 hover:text-green-700 p-1.5 rounded hover:bg-green-50"
                          >
                              <Save size={20} />
                          </button>
                      </div>
                  )}
              </div>
              
              <div className="p-6 space-y-6">
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                      <div className="space-y-1">
                          <label className="text-xs font-semibold text-gray-500 uppercase">Full Name</label>
                          {isEditing ? (
                              <div className="flex items-center gap-2 border rounded-lg px-3 py-2 bg-gray-50 focus-within:ring-2 ring-blue-500 focus-within:bg-white">
                                  <User size={18} className="text-gray-400" />
                                  <input 
                                    type="text" 
                                    value={formData.name}
                                    onChange={e => handleInputChange('name', e.target.value)}
                                    className="bg-transparent outline-none w-full text-sm"
                                  />
                              </div>
                          ) : (
                              <p className="text-gray-900 font-medium flex items-center gap-2 h-10">
                                  <User size={18} className="text-gray-400" /> {user.name}
                              </p>
                          )}
                      </div>

                      <div className="space-y-1">
                          <label className="text-xs font-semibold text-gray-500 uppercase">Email Address</label>
                          {isEditing ? (
                              <div className="flex items-center gap-2 border rounded-lg px-3 py-2 bg-gray-50 focus-within:ring-2 ring-blue-500 focus-within:bg-white">
                                  <Mail size={18} className="text-gray-400" />
                                  <input 
                                    type="email" 
                                    value={formData.email}
                                    onChange={e => handleInputChange('email', e.target.value)}
                                    className="bg-transparent outline-none w-full text-sm"
                                  />
                              </div>
                          ) : (
                              <p className="text-gray-900 font-medium flex items-center gap-2 h-10">
                                  <Mail size={18} className="text-gray-400" /> {user.email}
                              </p>
                          )}
                      </div>

                      <div className="space-y-1">
                          <label className="text-xs font-semibold text-gray-500 uppercase">Phone Number</label>
                          {isEditing ? (
                              <div className="flex items-center gap-2 border rounded-lg px-3 py-2 bg-gray-50 focus-within:ring-2 ring-blue-500 focus-within:bg-white">
                                  <Phone size={18} className="text-gray-400" />
                                  <input 
                                    type="text" 
                                    value={formData.phone}
                                    onChange={e => handleInputChange('phone', e.target.value)}
                                    className="bg-transparent outline-none w-full text-sm"
                                  />
                              </div>
                          ) : (
                              <p className="text-gray-900 font-medium flex items-center gap-2 h-10">
                                  <Phone size={18} className="text-gray-400" /> {user.phone}
                              </p>
                          )}
                      </div>

                      <div className="space-y-1">
                          <label className="text-xs font-semibold text-gray-500 uppercase">Branch / Unit</label>
                           {isEditing ? (
                              <div className="flex items-center gap-2 border rounded-lg px-3 py-2 bg-gray-50 focus-within:ring-2 ring-blue-500 focus-within:bg-white">
                                  <MapPin size={18} className="text-gray-400" />
                                  <input 
                                    type="text" 
                                    value={formData.branchId}
                                    onChange={e => handleInputChange('branchId', e.target.value)}
                                    className="bg-transparent outline-none w-full text-sm"
                                  />
                              </div>
                          ) : (
                              <p className="text-gray-900 font-medium flex items-center gap-2 h-10">
                                  <MapPin size={18} className="text-gray-400" /> {user.branchId}
                              </p>
                          )}
                      </div>
                  </div>
              </div>
           </div>

           {/* System Roles */}
           <div className="bg-white rounded-xl shadow-sm border border-gray-200 mt-6 p-6">
                <h3 className="text-lg font-bold text-gray-800 mb-4">System Roles & Permissions</h3>
                <div className="space-y-3">
                    <div className="flex items-center justify-between p-3 bg-gray-50 rounded-lg border border-gray-100">
                        <div>
                            <p className="font-semibold text-sm text-gray-800">Super Administrator</p>
                            <p className="text-xs text-gray-500">Full access to Core Banking, GL, and User Management.</p>
                        </div>
                        <BadgeCheck className="text-blue-600" size={20} />
                    </div>
                    <div className="flex items-center justify-between p-3 bg-gray-50 rounded-lg border border-gray-100">
                        <div>
                            <p className="font-semibold text-sm text-gray-800">Teller Supervisor</p>
                            <p className="text-xs text-gray-500">Can override limits and approve reversals.</p>
                        </div>
                        <BadgeCheck className="text-blue-600" size={20} />
                    </div>
                </div>
           </div>
        </div>

        {/* Change Password Modal */}
        {showPasswordModal && (
            <div className="fixed inset-0 z-50 bg-black/50 flex items-center justify-center p-4 backdrop-blur-sm">
                <div className="bg-white rounded-lg shadow-xl w-full max-w-sm p-6">
                    <div className="flex justify-between items-center mb-4">
                        <h3 className="font-bold text-lg text-gray-800 flex items-center gap-2">
                            <Key size={18} /> Change Password
                        </h3>
                        <button onClick={() => setShowPasswordModal(false)} className="text-gray-400 hover:text-gray-600">
                            <X size={20} />
                        </button>
                    </div>
                    <div className="space-y-4">
                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">New Password</label>
                            <input 
                                type="password" 
                                className="w-full border border-gray-300 rounded-lg p-2.5 text-sm focus:ring-2 focus:ring-blue-500 outline-none"
                                value={passwordForm.new}
                                onChange={e => setPasswordForm({ ...passwordForm, new: e.target.value })}
                            />
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">Confirm Password</label>
                            <input 
                                type="password" 
                                className="w-full border border-gray-300 rounded-lg p-2.5 text-sm focus:ring-2 focus:ring-blue-500 outline-none"
                                value={passwordForm.confirm}
                                onChange={e => setPasswordForm({ ...passwordForm, confirm: e.target.value })}
                            />
                        </div>
                        <button 
                            onClick={handleChangePasswordSubmit}
                            className="w-full bg-blue-600 text-white font-bold py-2 rounded-lg hover:bg-blue-700 mt-2"
                        >
                            Update Password
                        </button>
                    </div>
                </div>
            </div>
        )}
      </div>
    </div>
  );
};

export default UserProfile;
