import { describe, expect, it } from 'vitest';
import { Permissions } from '../../lib/Permissions';

describe('frontend permission catalog', () => {
  it('includes the newer reporting permissions exposed by the API', () => {
    expect(Permissions.Reports.Generate).toBe('reports.generate');
    expect(Permissions.Reports.Approve).toBe('reports.approve');
    expect(Permissions.Reports.Submit).toBe('reports.submit');
    expect(Permissions.Reports.Configure).toBe('reports.configure');
  });
});
