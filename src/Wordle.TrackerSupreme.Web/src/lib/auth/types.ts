export type Player = {
	id: string;
	displayName: string;
	email: string;
	createdOn: string;
};

export type AuthResponse = {
	player: Player;
	token: string;
};

export type AuthState = {
	user: Player | null;
	token: string | null;
	ready: boolean;
	error?: string | null;
};
