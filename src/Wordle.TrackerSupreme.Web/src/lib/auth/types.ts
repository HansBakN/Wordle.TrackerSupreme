export type Player = {
	id: string;
	displayName: string;
	email: string;
	createdOn: string;
	isAdmin: boolean;
};

export type AuthState = {
	user: Player | null;
	token: string | null;
	ready: boolean;
	error?: string | null;
};
