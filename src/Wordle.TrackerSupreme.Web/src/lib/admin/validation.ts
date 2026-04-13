export const displayNameMinLength = 2;
export const displayNameMaxLength = 200;
export const emailMaxLength = 320;
export const passwordMinLength = 6;
export const passwordMaxLength = 100;

const emailPattern = /^[^\s@]+@[^\s@]+$/;

export function getAdminProfileValidationError(displayName: string, email: string): string | null {
	const trimmedName = displayName.trim();
	const trimmedEmail = email.trim();

	if (!trimmedName) {
		return 'Display name is required.';
	}

	if (trimmedName.length < displayNameMinLength || trimmedName.length > displayNameMaxLength) {
		return `Display name must be between ${displayNameMinLength} and ${displayNameMaxLength} characters.`;
	}

	if (!trimmedEmail) {
		return 'Email is required.';
	}

	if (trimmedEmail.length > emailMaxLength) {
		return `Email cannot exceed ${emailMaxLength} characters.`;
	}

	if (!emailPattern.test(trimmedEmail)) {
		return 'Email must be a valid email address.';
	}

	return null;
}

export function getAdminPasswordValidationError(password: string): string | null {
	if (!password) {
		return 'Password is required.';
	}

	if (password.length < passwordMinLength || password.length > passwordMaxLength) {
		return `Password must be between ${passwordMinLength} and ${passwordMaxLength} characters.`;
	}

	return null;
}
