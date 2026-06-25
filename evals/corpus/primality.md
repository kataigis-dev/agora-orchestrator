# Primality testing

A natural number greater than 1 is prime if it has no positive divisors other than 1 and itself.
The numbers 0 and 1 are not prime, and every negative integer is non-prime by convention.

## Trial division up to the square root

The standard elementary method is trial division: to test whether n is prime, check whether any
integer d with 2 <= d <= sqrt(n) divides n. Checking only up to the square root of n is sufficient,
because if n = a * b then at least one of a or b is <= sqrt(n). This makes the test run in
O(sqrt(n)) time rather than O(n).

## The 6k +/- 1 optimization

After handling 2 and 3 directly, every prime greater than 3 is of the form 6k - 1 or 6k + 1.
So one can step the trial divisor by 6, testing i and i + 2 starting from i = 5, which skips all
multiples of 2 and 3 and roughly triples the speed of naive trial division.

## Edge cases

A correct is_prime function must return false for n < 2, handle 2 and 3 as prime, and reject even
numbers early. Booleans should not be treated as valid integer input in languages where bool is a
subtype of int.
