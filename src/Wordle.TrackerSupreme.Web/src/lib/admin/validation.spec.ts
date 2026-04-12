import { describe, expect, it } from 'vitest';
import { getAdminPasswordValidationError, getAdminProfileValidationError } from './validation';

describe('admin validation', () => {
	it('rejects invalid profile values', () => {
		expect(getAdminProfileValidationError(' ', 'admin@example.com')).toBe(
			'Display name is required.'
		);
		expect(getAdminProfileValidationError('Admin', 'invalid-email')).toBe(
			'Email must be a valid email address.'
		);
	});

	it('accepts valid profile values', () => {
		expect(getAdminProfileValidationError('Admin Supreme', 'admin@example.com')).toBeNull();
	});

	it('rejects short passwords', () => {
		expect(getAdminPasswordValidationError('abc')).toBe(
			'Password must be between 6 and 100 characters.'
		);
	});

	it('accepts valid passwords', () => {
		expect(getAdminPasswordValidationError('Reset!234')).toBeNull();
	});
});
