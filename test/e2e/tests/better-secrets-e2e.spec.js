import { expect, test } from 'vitest';
import { render } from 'cli-testing-library';
import path from 'node:path';
import 'cli-testing-library/vitest';

const __dirname = import.meta.dirname;

test('adds 1 + 2 to equal 3', async () => {
  const { clear, findByText, queryByText, userEvent, debug } = await render('dotnet', [
    'better-secrets',
  ], {
    cwd: path.join(__dirname, '../sample-project'),
    spawnOpts: {
      stdio: 0
    }
  });


  const firstScreen = await findByText('Karls Better Secrets Tool');

  expect(firstScreen).toBeInTheConsole();

  await userEvent.keyboard('{A}'); // Add secret

  const addSecretScreen = await findByText('Enter secret key:');

  expect(addSecretScreen).toBeInTheConsole();
})
