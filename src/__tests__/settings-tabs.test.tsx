// @ts-nocheck
import React from 'react';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import Settings from '../../components/Settings';
import { Role, StaffUser, SystemConfig } from '../../types';

const roles: Role[] = [
  {
    id: 'role-admin',
    name: 'Admin',
    description: 'System administrator',
    permissions: ['SYSTEM_ADMIN', 'SYSTEM_CONFIG'],
  },
  {
    id: 'role-ops',
    name: 'Operations',
    description: 'Operations team',
    permissions: ['USER_READ'],
  },
];

const users: StaffUser[] = [
  {
    id: 'user-1',
    name: 'Ava Doe',
    email: 'ava@example.com',
    phone: '555-111-2222',
    roleId: 'role-admin',
    roleName: 'Admin',
    branchId: 'BR-001',
    avatarInitials: 'AD',
    status: 'Active',
    lastLogin: '2026-02-26 09:00',
  },
];

const systemConfig: SystemConfig = {
  amlThreshold: 10000,
  kycLimits: {
    'Tier 1': { maxBalance: 2000, dailyLimit: 1000 },
    'Tier 2': { maxBalance: 10000, dailyLimit: 5000 },
    'Tier 3': { maxBalance: 50000, dailyLimit: 20000 },
  },
};

describe('Settings tabs', () => {
  it('renders user management tab by default', () => {
    render(
      <Settings
        users={users}
        roles={roles}
        systemConfig={systemConfig}
        onCreateRole={() => undefined}
        onUpdateUserRole={() => undefined}
        onCreateStaff={() => undefined}
        onResetPassword={() => undefined}
        onUpdateConfig={() => undefined}
      />
    );

    expect(screen.getByText('Staff Directory')).toBeInTheDocument();
    expect(screen.getByText('User Management')).toBeInTheDocument();
  });

  it('switches to Permissions tab', async () => {
    const user = userEvent.setup();

    render(
      <Settings
        users={users}
        roles={roles}
        systemConfig={systemConfig}
        onCreateRole={() => undefined}
        onUpdateUserRole={() => undefined}
        onCreateStaff={() => undefined}
        onResetPassword={() => undefined}
        onUpdateConfig={() => undefined}
      />
    );

    await user.click(screen.getByRole('button', { name: /Permissions/i }));

    expect(screen.getByText('Role Groups')).toBeInTheDocument();
  });

  it('switches to Authentication tab', async () => {
    const user = userEvent.setup();

    render(
      <Settings
        users={users}
        roles={roles}
        systemConfig={systemConfig}
        onCreateRole={() => undefined}
        onUpdateUserRole={() => undefined}
        onCreateStaff={() => undefined}
        onResetPassword={() => undefined}
        onUpdateConfig={() => undefined}
      />
    );

    await user.click(screen.getByRole('button', { name: /Authentication/i }));

    expect(screen.getByText('Authentication & Database')).toBeInTheDocument();
    expect(screen.getByText('Active Database Engine')).toBeInTheDocument();
  });

  it('switches to Process Designer tab', async () => {
    const user = userEvent.setup();

    render(
      <Settings
        users={users}
        roles={roles}
        systemConfig={systemConfig}
        onCreateRole={() => undefined}
        onUpdateUserRole={() => undefined}
        onCreateStaff={() => undefined}
        onResetPassword={() => undefined}
        onUpdateConfig={() => undefined}
        workflows={[]}
        onCreateWorkflow={() => undefined}
        onUpdateWorkflow={() => undefined}
      />
    );

    await user.click(screen.getByRole('button', { name: /Process Designer/i }));

    expect(screen.getByText('Processes')).toBeInTheDocument();
  });

  it('switches to Menu Config tab', async () => {
    const user = userEvent.setup();

    render(
      <Settings
        users={users}
        roles={roles}
        systemConfig={systemConfig}
        onCreateRole={() => undefined}
        onUpdateUserRole={() => undefined}
        onCreateStaff={() => undefined}
        onResetPassword={() => undefined}
        onUpdateConfig={() => undefined}
        onSaveMenu={() => undefined}
        onDeleteMenu={() => undefined}
        menuItems={[]}
      />
    );

    await user.click(screen.getByRole('button', { name: /Menu Config/i }));

    expect(screen.getByText('Custom Menus')).toBeInTheDocument();
  });

  it('switches to Configuration tab', async () => {
    const user = userEvent.setup();

    render(
      <Settings
        users={users}
        roles={roles}
        systemConfig={systemConfig}
        onCreateRole={() => undefined}
        onUpdateUserRole={() => undefined}
        onCreateStaff={() => undefined}
        onResetPassword={() => undefined}
        onUpdateConfig={() => undefined}
      />
    );

    await user.click(screen.getByRole('button', { name: /Configuration/i }));

    expect(screen.getByText('Transaction Limits')).toBeInTheDocument();
    expect(screen.getByText('KYC Tier Limits')).toBeInTheDocument();
  });
});
