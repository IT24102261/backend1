import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { HomePage } from './HomePage'

describe('HomePage', () => {
  it('renders the product title', () => {
    render(
      <MemoryRouter>
        <HomePage />
      </MemoryRouter>,
    )
    expect(screen.getAllByText('FixFlow AI').length).toBeGreaterThan(0)
    expect(screen.getByRole('heading', { name: /Trusted home/i })).toBeInTheDocument()
  })
})
