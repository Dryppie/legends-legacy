import { getChatSendErrorMessage } from './chat.component';

describe('getChatSendErrorMessage', () => {
  it('replaces transport failures with an actionable availability message', () => {
    const result = getChatSendErrorMessage(
      new TypeError(
        'Failed to complete negotiation with the server: TypeError: Failed to fetch',
      ),
    );

    expect(result).toBe(
      'Chat is temporarily unavailable. Check your connection and try again.',
    );
  });

  it('preserves useful account guidance', () => {
    const result = getChatSendErrorMessage(
      new Error('Register your account before writing in chat.'),
    );

    expect(result).toBe('Register your account before writing in chat.');
  });

  it('does not expose unknown technical errors', () => {
    const result = getChatSendErrorMessage(
      new Error('Internal transport implementation detail'),
    );

    expect(result).toBe("Your message couldn't be sent. Please try again.");
  });
});
