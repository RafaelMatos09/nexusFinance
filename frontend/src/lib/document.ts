export function onlyDigits(value: string) {
  return value.replace(/\D/g, '')
}

export function isValidDocument(value: string) {
  const digits = onlyDigits(value)
  if (digits.length === 11) return isCpf(digits)
  if (digits.length === 14) return isCnpj(digits)
  return false
}

function isCpf(digits: string) {
  if (new Set(digits).size === 1) return false
  const numbers = [...digits].map(Number)
  const first = checkDigit(numbers, 9)
  const second = checkDigit(numbers, 10)
  return numbers[9] === first && numbers[10] === second
}

function isCnpj(digits: string) {
  if (new Set(digits).size === 1) return false
  const numbers = [...digits].map(Number)
  const firstWeights = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]
  const secondWeights = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]
  return numbers[12] === weighted(numbers, firstWeights) && numbers[13] === weighted(numbers, secondWeights)
}

function checkDigit(numbers: number[], length: number) {
  let sum = 0
  for (let index = 0; index < length; index += 1) sum += numbers[index] * (length + 1 - index)
  const remainder = sum % 11
  return remainder < 2 ? 0 : 11 - remainder
}

function weighted(numbers: number[], weights: number[]) {
  const sum = weights.reduce((total, weight, index) => total + numbers[index] * weight, 0)
  const remainder = sum % 11
  return remainder < 2 ? 0 : 11 - remainder
}
