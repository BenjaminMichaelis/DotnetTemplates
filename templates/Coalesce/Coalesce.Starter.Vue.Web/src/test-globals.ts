/*
 * Set our test timezone to UTC to match CI
 *
 * See https://github.com/vitest-dev/vitest/issues/1575#issuecomment-1439286286
 */
export const setup = () => {
  process.env.TZ = "UTC";
};
