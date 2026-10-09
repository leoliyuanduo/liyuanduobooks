import AxeBuilder from '@axe-core/playwright';
import { expect, type Page, test } from '@playwright/test';

/** Titles in a card, in display order. */
async function titles(page: Page, heading: string): Promise<string[]> {
  return page.getByRole('region', { name: heading }).getByRole('listitem').allTextContents();
}

async function expectNoAccessibilityViolations(page: Page) {
  const results = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21aa', 'wcag22aa'])
    .analyze();
  expect(results.violations, JSON.stringify(results.violations, null, 2)).toEqual([]);
}

test.describe('Owners and Books', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('shows the title and waits for the user to ask for books', async ({ page }) => {
    await expect(page.getByRole('heading', { level: 1, name: 'Owners and Books' })).toBeVisible();
    await expect(page.getByText(/Select “Get Books”/)).toBeVisible();
    await expectNoAccessibilityViolations(page);
  });

  test('"Get Books" lists every book alphabetically under each age category', async ({ page }) => {
    await page.getByRole('button', { name: 'Get Books' }).click();

    await expect(page.getByRole('heading', { name: 'Books owned by Adults' })).toBeVisible();
    expect(await titles(page, 'Books owned by Adults')).toEqual([
      'Great Expectations',
      "Gulliver's Travels",
      'Hamlet',
      'Jane Eyre',
      'React: The Ultimate Guide',
      'Wuthering Heights',
    ]);
    expect(await titles(page, 'Books owned by Children')).toEqual([
      'Great Expectations',
      'Hamlet',
      'Little Red Riding Hood',
      'The Hobbit',
    ]);
    await expectNoAccessibilityViolations(page);
  });

  test('"Hardcover only" shows hardcovers with the hardcover headings', async ({ page }) => {
    await page.getByRole('button', { name: 'Hardcover only' }).click();

    await expect(page.getByRole('heading', { name: 'Hardcover Books owned by Adults' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Hardcover Books owned by Children' })).toBeVisible();
    expect(await titles(page, 'Hardcover Books owned by Children')).toEqual([
      'Great Expectations',
      'Little Red Riding Hood',
    ]);
    await expect(page).toHaveURL(/\?view=hardcover$/);
    await expectNoAccessibilityViolations(page);
  });

  test('the selected view survives a reload and Back returns to the previous view', async ({ page }) => {
    await page.getByRole('button', { name: 'Get Books' }).click();
    await expect(page.getByRole('heading', { name: 'Books owned by Adults' })).toBeVisible();
    await page.getByRole('button', { name: 'Hardcover only' }).click();
    await expect(page.getByRole('heading', { name: 'Hardcover Books owned by Adults' })).toBeVisible();

    await page.reload();
    await expect(page.getByRole('heading', { name: 'Hardcover Books owned by Adults' })).toBeVisible();

    await page.goBack();
    await expect(page.getByRole('heading', { name: 'Books owned by Adults' })).toBeVisible();
  });

  test('works with the keyboard alone', async ({ page }) => {
    await page.getByRole('button', { name: 'Get Books' }).focus();
    await page.keyboard.press('Enter');

    await expect(page.getByRole('heading', { name: 'Books owned by Adults' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Get Books' })).toHaveAttribute('aria-pressed', 'true');
  });
});

test.describe('Responsive layout (600px breakpoint)', () => {
  test('controls sit below the books: side by side on desktop, stacked on mobile', async ({
    page,
    isMobile,
  }) => {
    await page.goto('/?view=all');
    await expect(page.getByRole('heading', { name: 'Books owned by Adults' })).toBeVisible();

    const button = await page.getByRole('button', { name: 'Get Books' }).boundingBox();
    const link = await page.getByRole('button', { name: 'Hardcover only' }).boundingBox();
    const lastCard = await page.getByRole('region', { name: 'Books owned by Children' }).boundingBox();
    expect(button && link && lastCard).toBeTruthy();

    // Below the cards on every viewport.
    expect(button!.y).toBeGreaterThan(lastCard!.y + lastCard!.height);

    if (isMobile) {
      expect(link!.y).toBeGreaterThan(button!.y); // button above link
    } else {
      expect(Math.abs(link!.y - button!.y)).toBeLessThan(20); // same row
      expect(link!.x).toBeLessThan(button!.x); // link to the left of the button
    }
  });

  test('cards are side by side on desktop and stacked on mobile', async ({ page, isMobile }) => {
    await page.goto('/?view=all');
    await expect(page.getByRole('heading', { name: 'Books owned by Children' })).toBeVisible();
    const adults = await page.getByRole('region', { name: 'Books owned by Adults' }).boundingBox();
    const children = await page.getByRole('region', { name: 'Books owned by Children' }).boundingBox();

    if (isMobile) {
      expect(children!.y).toBeGreaterThan(adults!.y + adults!.height);
    } else {
      expect(Math.abs(children!.y - adults!.y)).toBeLessThan(5);
    }
  });
});

/**
 * The brief: "The mobile breakpoint is 600px". Test both sides of the line, not just
 * a phone and a wide desktop: a mistake such as min-width: 640px would pass those.
 */
test.describe('Exactly at the 600px breakpoint', () => {
  test.beforeEach(({ isMobile }) => {
    test.skip(isMobile, 'viewport widths are set explicitly; one browser profile is enough');
  });

  async function layoutAt(page: Page, width: number) {
    await page.setViewportSize({ width, height: 900 });
    await page.goto('/?view=all');
    await expect(page.getByRole('heading', { name: 'Books owned by Children' })).toBeVisible();

    const adults = (await page.getByRole('region', { name: 'Books owned by Adults' }).boundingBox())!;
    const children = (await page.getByRole('region', { name: 'Books owned by Children' }).boundingBox())!;
    const button = (await page.getByRole('button', { name: 'Get Books' }).boundingBox())!;
    const link = (await page.getByRole('button', { name: 'Hardcover only' }).boundingBox())!;

    return {
      cardsSideBySide: Math.abs(children.y - adults.y) < 5,
      controlsInOneRow: Math.abs(link.y - button.y) < 20,
    };
  }

  test('599px wide uses the mobile layout (stacked)', async ({ page }) => {
    expect(await layoutAt(page, 599)).toEqual({ cardsSideBySide: false, controlsInOneRow: false });
  });

  test('600px wide uses the desktop layout (side by side)', async ({ page }) => {
    expect(await layoutAt(page, 600)).toEqual({ cardsSideBySide: true, controlsInOneRow: true });
  });
});

test.describe('Production hardening', () => {
  test('the page is served with security headers', async ({ request }) => {
    const response = await request.get('/');

    expect(response.status()).toBe(200);
    expect(response.headers()['content-security-policy']).toContain("default-src 'self'");
    expect(response.headers()['x-content-type-options']).toBe('nosniff');
    expect(response.headers()['server']).toBeUndefined();
  });

  test('the API rejects requests outside its contract with problem details', async ({ request }) => {
    const response = await request.get('/api/v1/books?ageCategory=teen');

    expect(response.status()).toBe(400);
    expect(response.headers()['content-type']).toContain('application/problem+json');
  });

  test('health endpoints answer for the orchestrator', async ({ request }) => {
    expect((await request.get('/health/live')).status()).toBe(200);
    expect((await request.get('/health/ready')).status()).toBe(200);
  });
});
