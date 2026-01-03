import { CONSTANTS } from './constants';

export const buildApiUrl = (path: string) => `${CONSTANTS.API_BASE}${path}`;

