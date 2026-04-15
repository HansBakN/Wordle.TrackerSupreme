export const defaultConfettiPieceCount = 60;

export type ConfettiPiece = {
	id: number;
	dx: number;
	dy: number;
	rotation: number;
	hue: number;
	delay: number;
	duration: number;
	size: number;
};

export function buildConfettiPieces(
	count: number,
	random: () => number = Math.random
): ConfettiPiece[] {
	const pieces: ConfettiPiece[] = [];
	for (let i = 0; i < count; i += 1) {
		const dx = Math.round((random() - 0.5) * 420);
		const dy = Math.round((random() - 0.15) * 340);
		pieces.push({
			id: i,
			dx,
			dy,
			rotation: Math.round(random() * 360),
			hue: Math.round(random() * 360),
			delay: Math.round(random() * 180),
			duration: 900 + Math.round(random() * 650),
			size: 8 + Math.round(random() * 6)
		});
	}
	return pieces;
}
