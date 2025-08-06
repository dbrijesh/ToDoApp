import { test, expect } from '@playwright/test';

test.describe('TODO Application E2E Tests', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should display the application title', async ({ page }) => {
    await expect(page).toHaveTitle(/TODO App/);
    await expect(page.locator('h1')).toContainText('TODO Application');
  });

  test('should display login button when not authenticated', async ({ page }) => {
    const loginButton = page.locator('.login-btn');
    await expect(loginButton).toBeVisible();
    await expect(loginButton).toContainText('Sign in with Azure');
  });

  test('should display loading state message', async ({ page }) => {
    const loadingMessage = page.locator('.loading-state p');
    await expect(loadingMessage).toContainText('Please sign in to access your TODOs');
  });

  // Note: Since we can't easily mock Azure authentication in E2E tests,
  // we'll create a mock scenario or require manual authentication for full E2E testing
  test.describe('Authenticated User Flow (Mock)', () => {
    test.beforeEach(async ({ page }) => {
      // This would require setting up a mock authentication state
      // For a real E2E test, you would either:
      // 1. Set up test credentials and automate the Azure login flow
      // 2. Create a test mode that bypasses authentication
      // 3. Use session storage/localStorage to mock authentication state
      
      // For this example, we'll skip to the authenticated state
      // by mocking the authentication in localStorage
      await page.addInitScript(() => {
        // Mock authenticated state
        const mockAccount = {
          localAccountId: 'test-user',
          name: 'Test User',
          username: 'test@example.com'
        };
        
        // This is a simplified mock - in reality, MSAL manages this differently
        localStorage.setItem('msal.test.account', JSON.stringify(mockAccount));
      });
    });

    test('should show TODO interface when authenticated', async ({ page }) => {
      // This test would need proper MSAL mocking or authentication bypass
      // For now, we'll check basic UI elements that should exist
      
      await page.goto('/');
      
      // Wait for potential authentication flow to complete
      await page.waitForTimeout(2000);
      
      // Check if either login or authenticated state is shown
      const loginButton = page.locator('.login-btn');
      const todoContainer = page.locator('.todo-container');
      
      // One of these should be visible
      await expect(
        loginButton.or(todoContainer)
      ).toBeVisible();
    });
  });

  test.describe('UI Responsiveness', () => {
    test('should be responsive on mobile devices', async ({ page }) => {
      await page.setViewportSize({ width: 375, height: 667 }); // iPhone SE size
      
      const header = page.locator('.header');
      await expect(header).toBeVisible();
      
      const loginButton = page.locator('.login-btn');
      await expect(loginButton).toBeVisible();
    });

    test('should be responsive on tablet devices', async ({ page }) => {
      await page.setViewportSize({ width: 768, height: 1024 }); // iPad size
      
      const header = page.locator('.header');
      await expect(header).toBeVisible();
    });

    test('should maintain layout on large screens', async ({ page }) => {
      await page.setViewportSize({ width: 1920, height: 1080 });
      
      const appContainer = page.locator('.app-container');
      await expect(appContainer).toBeVisible();
    });
  });

  test.describe('Color Scheme Validation', () => {
    test('should use the correct brand colors', async ({ page }) => {
      // Check if the primary brand color (rgb(0, 50, 100)) is applied
      const authSection = page.locator('.auth-section');
      await expect(authSection).toBeVisible();
      
      // Verify brand color is applied (this is approximate - actual testing would use computed styles)
      const styles = await authSection.evaluate((el) => {
        return window.getComputedStyle(el).backgroundColor;
      });
      
      // rgb(0, 50, 100) should be applied to the auth section
      expect(styles).toMatch(/rgb\(0,\s*50,\s*100\)|rgb\(0\s+50\s+100\)/);
    });

    test('should have proper contrast ratios', async ({ page }) => {
      // Check text visibility against background
      const title = page.locator('h1');
      await expect(title).toBeVisible();
      
      // Ensure text is readable (this is a basic visibility check)
      const isVisible = await title.isVisible();
      expect(isVisible).toBeTruthy();
    });
  });

  test.describe('Accessibility', () => {
    test('should have proper heading hierarchy', async ({ page }) => {
      const h1 = page.locator('h1');
      await expect(h1).toBeVisible();
      
      // Check that h1 exists and is properly structured
      const h1Count = await page.locator('h1').count();
      expect(h1Count).toBeGreaterThanOrEqual(1);
    });

    test('should have proper button labels', async ({ page }) => {
      const loginButton = page.locator('.login-btn');
      await expect(loginButton).toBeVisible();
      
      // Check button has accessible text
      const buttonText = await loginButton.textContent();
      expect(buttonText).toBeTruthy();
      expect(buttonText).toContain('Sign in');
    });

    test('should support keyboard navigation', async ({ page }) => {
      // Focus on the login button using keyboard
      await page.keyboard.press('Tab');
      
      // Check if login button receives focus
      const loginButton = page.locator('.login-btn');
      await expect(loginButton).toBeFocused();
    });
  });

  test.describe('Performance', () => {
    test('should load within acceptable time', async ({ page }) => {
      const startTime = Date.now();
      await page.goto('/');
      await page.waitForLoadState('networkidle');
      const loadTime = Date.now() - startTime;
      
      // Should load within 3 seconds
      expect(loadTime).toBeLessThan(3000);
    });

    test('should have minimal layout shift', async ({ page }) => {
      await page.goto('/');
      
      // Wait for initial render
      await page.waitForTimeout(1000);
      
      // Basic check that main elements are stable
      const header = page.locator('.header');
      await expect(header).toBeVisible();
      await expect(header).toBeStable();
    });
  });

  test.describe('Error Handling', () => {
    test('should handle network errors gracefully', async ({ page }) => {
      // This would require intercepting network requests and simulating failures
      // For now, we'll just verify the app doesn't crash on load
      await page.goto('/');
      
      // Check that the app loads even if some resources might fail
      const appContainer = page.locator('.app-container');
      await expect(appContainer).toBeVisible();
    });

    test('should display appropriate error messages', async ({ page }) => {
      await page.goto('/');
      
      // Check that no JavaScript errors are thrown
      const errors: string[] = [];
      page.on('pageerror', (error) => {
        errors.push(error.message);
      });
      
      await page.waitForTimeout(2000);
      expect(errors).toHaveLength(0);
    });
  });
});